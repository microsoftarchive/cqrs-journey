using System;
using System.Collections.Generic;
using Conference.Common.Utils;
using Infrastructure.Sql.EventSourcing;
using TechTalk.SpecFlow;
using System.Linq;

namespace Conference.Specflow.Steps.Discounts {
    [Scope(Tag = "discount")]
    [Binding]
    internal class DiscountSteps {
        private DiscountDomain _discountDomainDomain;
        private Guid _confId;
        private string _discountSlug;

        public DiscountSteps() { _discountDomainDomain = new DiscountDomain(); }

        [Given(@"the event of creating a conference has occurred")]
        public void GivenTheEventOfCreatingAConferenceWithTheCodeHasOccurred() {
            _confId = Guid.NewGuid();
            _discountDomainDomain.Hydrate(new ConfCreated {ID = _confId});
        }

        [Given(@"the event of adding a discount with scope all for ([1-9]|([1-9][0-9]|100)) % has occurred")]
        public void GivenTheEventOfAddingADiscountWithScopeAllForAmountUnderCodeHasOccurred(int discount) {
            _discountSlug = Slug.NewSlug(7);
            _discountDomainDomain.Hydrate(new DiscountAdded {ConfID = _confId, DiscountSlug = _discountSlug, Amount = discount});
        }

        [When(@"the command to apply this discount to a total of \$\d+(\d\d){0,1} is received")]
        public void WhenTheCommandToApplyThisDiscountToATotalIsReceived(decimal total) {
            _discountDomainDomain.Consume(new ApplyDiscountCommand {})
        }

        [Then(@"the event of total changed to \$\d+(\d\d){0,1} is emmitted")]
        public void ThenTheEventOfTotalChangedToDiscountedTotalIsEmmitted(decimal discountedTotal) {
            
        }
    }

    internal class Slug { public static string NewSlug(int length) { HandleGenerator.Generate(length); } }

    internal class DiscountAdded : Event {
        public Guid ConfID;
        public string DiscountSlug;
        public int Amount;
    }

    internal class ConfCreated : Event{
        public Guid ID;
    }

    internal class DiscountDomain : IHydrate {
        private Dictionary<Guid, List<Event>> _conferences;
        private DiscountDomain() { _conferences = new Dictionary<Guid, List<Event>>();  }
        public void Hydrate(Event @event) {  if (@event is ConfCreated) Hydrate((ConfCreated)@event); }
        private void Hydrate(ConfCreated confCreated) {  _conferences[confCreated.ID] = new List<Event>(); }

    }

    internal interface IHydrate {
        void Hydrate(Event @event);
    }
}