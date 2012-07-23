// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Azure.Tests
{
    using Xunit;

    public class DynamicThrottlingFixture
    {
        [Fact]
        public void starts_low()
        {
            using (var sut = new DynamicThrottling(100, 10, 3, 5, 1, 8000))
            {
                Assert.Equal(10, sut.AvailableDegreesOfParallelism);
            }
        }

        [Fact]
        public void increases_on_completed_work()
        {
            using (var sut = new DynamicThrottling(100, 10, 3, 5, 1, 8000))
            {
                sut.NotifyWorkStarted();
                var startingValue = sut.AvailableDegreesOfParallelism;

                sut.NotifyWorkCompleted();

                Assert.True(startingValue < sut.AvailableDegreesOfParallelism);
            }
        }

        [Fact]
        public void continually_increases_on_completed_work()
        {
            using (var sut = new DynamicThrottling(100, 10, 3, 5, 1, 8000))
            {
                for (int i = 0; i < 10; i++)
                {
                    sut.NotifyWorkStarted();
                    var startingValue = sut.AvailableDegreesOfParallelism;
                    sut.NotifyWorkCompleted();
                    Assert.True(startingValue < sut.AvailableDegreesOfParallelism);
                }
            }
        }

        [Fact]
        public void decreases_on_penalize()
        {
            using (var sut = new DynamicThrottling(100, 10, 3, 5, 1, 8000))
            {
                IncreaseDegreesOfParallelism(sut);

                sut.NotifyWorkStarted();
                var startingValue = sut.AvailableDegreesOfParallelism;
                sut.Penalize();

                Assert.True(startingValue > sut.AvailableDegreesOfParallelism);
            }
        }

        [Fact]
        public void penalize_decreases_less_than_completed_with_error()
        {
            using (var sut1 = new DynamicThrottling(100, 10, 3, 5, 1, 8000))
            using (var sut2 = new DynamicThrottling(100, 10, 3, 5, 1, 8000))
            {
                IncreaseDegreesOfParallelism(sut1);
                IncreaseDegreesOfParallelism(sut2);

                sut1.NotifyWorkStarted();
                sut2.NotifyWorkStarted();

                sut1.Penalize();
                sut2.NotifyWorkCompletedWithError();

                Assert.True(sut1.AvailableDegreesOfParallelism > sut2.AvailableDegreesOfParallelism);
            }
        }

        private static void IncreaseDegreesOfParallelism(DynamicThrottling sut)
        {
            for (int i = 0; i < 10; i++)
            {
                // increase degrees to avoid being in the minimum boundary
                sut.NotifyWorkStarted();
                sut.NotifyWorkCompleted();
            }
        }
    }
}
