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

    public class DynamicThrottling : IDisposable
    {
        // configuration

        /// <summary>
        /// Maximum number of parallel jobs.
        /// </summary>
        private readonly int maxDegreeOfParallelism;

        /// <summary>
        /// Minimum number of parallel jobs.
        /// </summary>
        private readonly int minDegreeOfParallelism;

        /// <summary>
        /// Number of degrees of parallelism to remove on retrying.
        /// </summary>
        private readonly int retryParallelismPenalty;

        /// <summary>
        /// Number of degrees of parallelism to remove when work fails.
        /// </summary>
        private readonly int workFailedParallelismPenalty;

        /// <summary>
        /// Number of degrees of parallelism to restore on work completed.
        /// </summary>
        private readonly int workCompletedParallelismGain;

        /// <summary>
        /// Interval in milliseconds to restore 1 degree of parallelism.
        /// </summary>
        private readonly int intervalForRestoringDegreeOfParallelism;

        private readonly AutoResetEvent waitHandle = new AutoResetEvent(true);
        private readonly Timer parallelismRestoringTimer;

        private int currentParallelJobs = 0;
        private int availableDegreesOfParallelism;

        public DynamicThrottling(
            int maxDegreeOfParallelism, 
            int minDegreeOfParallelism,
            int retryParallelismPenalty,
            int workFailedParallelismPenalty,
            int workCompletedParallelismGain,
            int intervalForRestoringDegreeOfParallelism)
        {
            this.maxDegreeOfParallelism = maxDegreeOfParallelism;
            this.minDegreeOfParallelism = minDegreeOfParallelism;
            this.retryParallelismPenalty = retryParallelismPenalty;
            this.workFailedParallelismPenalty = workFailedParallelismPenalty;
            this.workCompletedParallelismGain = workCompletedParallelismGain;
            this.intervalForRestoringDegreeOfParallelism = 8000;
            this.parallelismRestoringTimer = new Timer(s => this.IncrementDegreesOfParallelism(1));

            this.availableDegreesOfParallelism = (maxDegreeOfParallelism + minDegreeOfParallelism) / 2;
        }

        public void WaitUntilAllowedParallelism(CancellationToken cancellationToken)
        {
            while (this.currentParallelJobs >= this.availableDegreesOfParallelism)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Trace.WriteLine("Waiting for available degrees of parallelism. Available: " + this.availableDegreesOfParallelism + ". In use: " + this.currentParallelJobs);

                this.waitHandle.WaitOne();
            }
        }

        public void NotifyWorkCompleted()
        {
            Interlocked.Decrement(ref this.currentParallelJobs);
            // Trace.WriteLine("Job finished. Parallel jobs are now: " + this.currentParallelJobs);
            IncrementDegreesOfParallelism(workCompletedParallelismGain);
        }

        public void NotifyWorkStarted()
        {
            Interlocked.Increment(ref this.currentParallelJobs);
            // Trace.WriteLine("Job started. Parallel jobs are now: " + this.currentParallelJobs);
        }

        public void OnRetrying()
        {
            // Slightly penalize with removal of some degrees of parallelism.
            this.DecrementDegreesOfParallelism(retryParallelismPenalty);
        }

        public void NotifyWorkCompletedWithError()
        {
            // Largely penalize with removal of several degrees of parallelism.
            this.DecrementDegreesOfParallelism(workFailedParallelismPenalty);
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

            this.parallelismRestoringTimer.Change(intervalForRestoringDegreeOfParallelism, intervalForRestoringDegreeOfParallelism);
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

        private void IncrementDegreesOfParallelism(int count)
        {
            if (this.availableDegreesOfParallelism < maxDegreeOfParallelism)
            {
                this.availableDegreesOfParallelism += count;
                if (this.availableDegreesOfParallelism >= maxDegreeOfParallelism)
                {
                    this.availableDegreesOfParallelism = maxDegreeOfParallelism;
                    // Trace.WriteLine("Incremented available degrees of parallelism. Available: " + this.availableDegreesOfParallelism);
                }
            }

            this.waitHandle.Set();
        }

        private void DecrementDegreesOfParallelism(int count)
        {
            this.availableDegreesOfParallelism -= count;
            if (this.availableDegreesOfParallelism < minDegreeOfParallelism)
            {
                this.availableDegreesOfParallelism = minDegreeOfParallelism;
            }
            // Trace.WriteLine("Decremented available degrees of parallelism. Available: " + this.availableDegreesOfParallelism);
        }
    }
}
