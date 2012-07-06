// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Azure
{
    using System;
    using System.Threading;

    /// <summary>
    /// Provides a way to throttle the work depending on the number of jobs it is able to complete and whether
    /// the job is penalized for trying to parallelize too many jobs.
    /// It uses a logarithmic growth function.
    /// </summary>
    public class DynamicThrottling : IDisposable
    {
        private const long MaxValue = 10000000000000;

        /// <summary>
        /// The growth function to determine available degree of parallelism.
        /// </summary>
        private readonly Func<long, int> growthFunction;
        private readonly double penalizeFactorSubtraction;
        private readonly double workFailedPenaltyFactorSubtraction;
        private readonly int intervalForRestoringParallelism;

        private readonly AutoResetEvent waitHandle = new AutoResetEvent(true);
        private readonly Timer parallelismRestoringTimer;

        private int currentParallelJobs = 0;
        private long currentValue;
        /// <summary>
        /// Initializes a new instance of <see cref="DynamicThrottling"/>.
        /// </summary>
        /// <param name="minDegreeOfParallelism">The minimum degree of parallelism.</param>
        /// <param name="logProductConstant">The constant to use when calculating the logarithmic growth.</param>
        /// <param name="penalizeFactor">The factor used to slightly penalize throttling.</param>
        /// <param name="workFailedPenaltyFactor">The factor used to heavily penalize throttling when work completed with failure.</param>
        /// <param name="intervalForRestoringParallelism">Time in milliseconds to restore 1 parallelism value.</param>
        public DynamicThrottling(
            int minDegreeOfParallelism,
            int logProductConstant,
            double penalizeFactor,
            double workFailedPenaltyFactor,
            int intervalForRestoringParallelism)
        {
            this.growthFunction = GetLogarithmicGrowth(minDegreeOfParallelism, logProductConstant);
            if (penalizeFactor > 1) throw new ArgumentOutOfRangeException("penalizeFactor");
            if (workFailedPenaltyFactor > 1) throw new ArgumentOutOfRangeException("workFailedPenaltyFactor");
            
            this.penalizeFactorSubtraction = 1 - penalizeFactor;
            this.workFailedPenaltyFactorSubtraction = 1- workFailedPenaltyFactor;
            this.intervalForRestoringParallelism = intervalForRestoringParallelism;
            
            this.parallelismRestoringTimer = new Timer(s => this.OnRestoringTimerTick());

            this.currentValue = 1;
        }

        public static Func<long, int> GetLogarithmicGrowth(int minimum, double constant)
        {
            if (minimum < 1) throw new ArgumentOutOfRangeException("minimum");
            if (constant < 0) throw new ArgumentOutOfRangeException("constant");

            var offset = -LogarithmicGrowth(0, constant, 1) + minimum;
            return (long value) => LogarithmicGrowth(offset, constant, value > 1 ? value : 1);
        }

        private static Func<double, double, long, int> LogarithmicGrowth =
            (offset, constant, value) => (int)(offset + (constant * Math.Log(value + 4)));


        public int AvailableDegreesOfParallelism
        {
            get { return this.growthFunction(this.currentValue); }
        }

        public void WaitUntilAllowedParallelism(CancellationToken cancellationToken)
        {
            while (this.currentParallelJobs >= this.AvailableDegreesOfParallelism)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                this.waitHandle.WaitOne();
            }
        }

        public void NotifyWorkCompleted()
        {
            Interlocked.Decrement(ref this.currentParallelJobs);
            if (this.currentValue < MaxValue)
            {
                this.currentValue++;
            }

            this.waitHandle.Set();
        }

        public void NotifyWorkStarted()
        {
            Interlocked.Increment(ref this.currentParallelJobs);
            // Trace.WriteLine("Job started. Parallel jobs are now: " + this.currentParallelJobs);
        }

        public void Penalize()
        {
            // Slightly penalize with removal of some degrees of parallelism.
            this.DecrementCurrentValue(penalizeFactorSubtraction);
        }

        public void NotifyWorkCompletedWithError()
        {
            // Largely penalize with removal of several degrees of parallelism.
            this.DecrementCurrentValue(workFailedPenaltyFactorSubtraction);
            Interlocked.Decrement(ref this.currentParallelJobs);
            // Trace.WriteLine("Job finished with error. Parallel jobs are now: " + this.currentParallelJobs);
            this.waitHandle.Set();
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => this.parallelismRestoringTimer.Change(Timeout.Infinite, Timeout.Infinite));
            }

            this.parallelismRestoringTimer.Change(intervalForRestoringParallelism, intervalForRestoringParallelism);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.waitHandle.Dispose();
                this.parallelismRestoringTimer.Dispose();
            }
        }

        private void OnRestoringTimerTick()
        {
            // Slightly restore degrees of parallelism if the value is low.
            if (this.currentValue < 100)
            {
                this.currentValue++;
            }

            this.waitHandle.Set();
        }

        private void DecrementCurrentValue(double penalty)
        {
            this.currentValue = (long)(((double)this.currentValue) * penalty);
            if (this.currentValue < 1)
            {
                this.currentValue = 1;
            }
        }
    }
}
