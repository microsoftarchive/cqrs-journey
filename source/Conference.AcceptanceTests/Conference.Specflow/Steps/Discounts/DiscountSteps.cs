using System;
using System.Collections.Generic;
using Conference.Specflow.Support;
using Discounts;
using Discounts.Commands;
using Discounts.Events;
using Infrastructure.EventSourcing;
using TechTalk.SpecFlow;
using Xunit;
using System.Linq;

namespace Conference.Specflow.Steps.Discounts {
    [Scope(Tag = "discount")]
    [Binding]
    internal class DiscountSteps {
        private const int ArbitraryPercantage = 0;
        private const int ArbitraryDiscountAmount = 200;
        private const string ArbitraryCode = "ABC";
        private readonly DiscountDomain _discountDomain;
        private Guid _confID;
        private Guid _discountID;
        private Guid _discountsForConfID = Guid.Empty;
        private List<IVersionedEvent> _generatedEvents;
        private Guid _currentOrder = Guid.Empty;
        private Exception _lastThrownFromWhen;
        private ConferenceDiscountsRepo _conferenceDiscountsRepo;

        public DiscountSteps() {
            _conferenceDiscountsRepo = new ConferenceDiscountsRepo();
            _discountDomain = new DiscountDomain(_conferenceDiscountsRepo);}

        [Given(@"the event of creating a conference has occurred")]
        public void GivenTheEventOfCreatingAConferenceWithTheCodeHasOccurred() {
            _confID = Guid.NewGuid();
            _discountsForConfID = Guid.NewGuid();
            _conferenceDiscountsRepo.Store(new ConfCreated {ID = _discountsForConfID, ConfID = _confID});
        }
        [Given(@"the event of adding a discount with scope all for ([1-9]|(?:[1-9][0-9])|100) % has occurred")]
        public void GivenTheEventOfAddingADiscountWithScopeAllForAmountUnderCodeHasOccurred(int discount) {
            _discountID = Guid.NewGuid();
            _conferenceDiscountsRepo.Store(new GlobalDiscountAddedEvent {ID = _discountsForConfID, DiscountID = _discountID, Percentage = discount});
        }
        [Given(@"the event of adding a discount has occurred")]
        public void GivenTheEventOfAddingADiscountHasOccurred()
        {
            _discountID = Guid.NewGuid();
            _conferenceDiscountsRepo.Store(new GlobalDiscountAddedEvent {ID = _discountsForConfID, DiscountID = _discountID, Percentage = ArbitraryPercantage});
        }
        [Given(@"the event of redeeming this discount has occurred")]
        public void GivenTheEventOfRedeemingThisDiscountHasOccurred() {
            _conferenceDiscountsRepo.Store(new DiscountAppliedEvent {ID = _discountsForConfID, DiscountID = _discountID, DiscountAmount = ArbitraryDiscountAmount, Order = CurrentOrder});
        }
        [When(@"the command to create a discount is received")]
        public void WhenTheCommandToCreateADiscountIsReceived()
        {   
            try {
                _generatedEvents = _discountDomain.Consume(new AddDiscountCommand {ID = _discountsForConfID, Code = ArbitraryCode, Percentage = ArbitraryPercantage}).ToList();
            }
            catch(Exception e) { _lastThrownFromWhen = e; }
        }

        [When(@"the command to apply this discount to a total of \$(\d+(?:\.\d\d){0,1}) is received")]
        public void WhenTheCommandToApplyThisDiscountToATotalIsReceived(decimal total) {
            try {
                _generatedEvents = _discountDomain.Consume(new ApplyDiscountCommand {ID = _discountsForConfID, DiscountID = _discountID, Total = total, Order = CurrentOrder}).ToList();
            }
            catch (Exception e) { _lastThrownFromWhen = e; }
        }

        [When(@"the command to apply this discount to any total is received")]
        public void WhenTheCommandToApplyThisDiscountToAnyTotalIsReceived() {
            try {
                _generatedEvents = _discountDomain.Consume(new ApplyDiscountCommand {
                    ID = _discountsForConfID, DiscountID = _discountID, Total = new Random(DateTime.Now.Millisecond).Next(10, 10000), Order = CurrentOrder }).ToList();
            }
            catch (Exception e) { _lastThrownFromWhen = e; }
        }

        [Then(@"the event of that discount being created is emmitted")]
        public void ThenTheEventOfThatDiscountBeingCreatedIsEmmitted()
        {
            Assert.Equal(_discountID, ((DiscountAddedEvent) _generatedEvents.ToList()[0]).DiscountID);
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