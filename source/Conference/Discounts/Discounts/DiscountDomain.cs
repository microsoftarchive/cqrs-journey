using System.Collections.Generic;
using Discounts.Commands;
using Discounts.Events;
using Discounts.Exceptions;
using Discounts.Infrastructure;
using Infrastructure.EventSourcing;

namespace Discounts {
    public class DiscountDomain : IConsume {
        private readonly IEventSourcedRepository<ConferenceDiscounts> _repository;
        public DiscountDomain(IEventSourcedRepository<ConferenceDiscounts> repository) { _repository = repository; }

        public IEnumerable<IVersionedEvent> Consume(DiscountCommand discountCommand) {
            var conferenceDiscounts = _repository.Get(discountCommand.ID);
            foreach (var discountEvent in ExecuteCorrespondingMethodAndReturnResultantEvents(discountCommand, conferenceDiscounts)) yield return AddContext(discountCommand, discountEvent);
            _repository.Save(conferenceDiscounts);
        }
        private static IEnumerable<DiscountEvent> ExecuteCorrespondingMethodAndReturnResultantEvents(DiscountCommand discountCommand, ConferenceDiscounts conferenceDiscounts) {
            if (discountCommand is ApplyDiscountCommand) return conferenceDiscounts.Consume((ApplyDiscountCommand) discountCommand);
            if (discountCommand is AddDiscountCommand) return conferenceDiscounts.Consume((AddDiscountCommand) discountCommand);
            throw new CommandNotHandledException();
        }
        private static DiscountEvent AddContext(DiscountCommand discountCommand, DiscountEvent discountEvent) {
            discountEvent.ID = discountCommand.ID;
            return discountEvent;
        }
    }
}