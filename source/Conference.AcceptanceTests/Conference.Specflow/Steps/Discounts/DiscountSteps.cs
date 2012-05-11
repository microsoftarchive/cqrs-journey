using System;
using System.Collections.Generic;
using Discounts;
using Discounts.Commands;
using Discounts.Events;
using Infrastructure.Sql.EventSourcing;
using TechTalk.SpecFlow;
using Xunit;
using System.Linq;

namespace Conference.Specflow.Steps.Discounts {
    [Scope(Tag = "discount")]
    [Binding]
    internal class DiscountSteps {
        private const int ArbitraryPercantage = 0;
        private const int ArbitraryDiscountAmount = 200;
        private DiscountDomain _discountDomain;
        private Guid _confId;
        private Guid _discountID;
        private IEnumerable<Event> _generatedEvents;
        private Guid _currentOrder = Guid.Empty;
        private Exception _lastThrownFromWhen;
        public DiscountSteps() { _discountDomain = new DiscountDomain(); }

        [Given(@"the event of creating a conference has occurred")]
        public void GivenTheEventOfCreatingAConferenceWithTheCodeHasOccurred() {
            _confId = Guid.NewGuid();
            _discountDomain.Store(new ConfCreated {ConfID = _confId});
        }
        [Given(@"the event of adding a discount with scope all for ([1-9]|(?:[1-9][0-9])|100) % has occurred")]
        public void GivenTheEventOfAddingADiscountWithScopeAllForAmountUnderCodeHasOccurred(int discount) {
            _discountID = Guid.NewGuid();
            _discountDomain.Store(new GlobalDiscountAddedEvent {DiscountID = _discountID, ConfID = _confId, Percentage = discount});
        }
        [Given(@"the event of adding a discount has occurred")]
        public void GivenTheEventOfAddingADiscountHasOccurred()
        {
            _discountID = Guid.NewGuid();
            _discountDomain.Store(new GlobalDiscountAddedEvent {DiscountID = _discountID, ConfID = _confId, Percentage = ArbitraryPercantage});
        }
        [Given(@"the event of redeeming this discount has occurred")]
        public void GivenTheEventOfRedeemingThisDiscountHasOccurred() {
            _discountDomain.Store(new DiscountAppliedEvent {ConfID = _confId, DiscountID = _discountID, DiscountAmount = ArbitraryDiscountAmount, Order = CurrentOrder});
        }

        [When(@"the command to apply this discount to a total of \$(\d+(?:\.\d\d){0,1}) is received")]
        public void WhenTheCommandToApplyThisDiscountToATotalIsReceived(decimal total) {
            try {
                _generatedEvents = _discountDomain.Consume(new ApplyDiscountCommand {ConfID = _confId, DiscountID = _discountID, Total = total, Order = CurrentOrder}).ToList();
            }
            catch (Exception e) { _lastThrownFromWhen = e; }
        }

        [When(@"the command to apply this discount to any total is received")]
        public void WhenTheCommandToApplyThisDiscountToAnyTotalIsReceived() {
            try {
                _generatedEvents = _discountDomain.Consume(new ApplyDiscountCommand {
                    ConfID = _confId, DiscountID = _discountID, Total = new Random(DateTime.Now.Millisecond).Next(10, 10000), Order = CurrentOrder }).ToList();
            }
            catch (Exception e) { _lastThrownFromWhen = e; }
        }

        [Then(@"the event \$(\d+(?:\d\d){0,1}) discount has been applied is emmitted")]
        public void ThenTheEventOfTotalChangedToDiscountedTotalIsEmmitted(decimal discountedTotal) {
            Assert.Equal(discountedTotal, ((DiscountAppliedEvent) _generatedEvents.ToList()[0]).DiscountAmount);
        }

        [Then(@"the event corresponds to the discount requested")]
        public void CurrentDiscount() {
            Assert.Equal(_discountID, ((DiscountAppliedEvent) _generatedEvents.ToList()[0]).DiscountID);
        }

        [Then(@"a (.+) is raised")]
        public void ThenAParticularExceptionIsRaised(string exceptionType) {
            if (_lastThrownFromWhen == null) throw new NoExceptionWasThrownException();
            Assert.Equal(exceptionType, _lastThrownFromWhen.GetType().ToString());
        }

        protected Guid CurrentOrder {
            get {
                if (_currentOrder == Guid.Empty) _currentOrder = Guid.NewGuid();
                return _currentOrder;
            }
        }
    }

    internal class NoExceptionWasThrownException : Exception {}
}