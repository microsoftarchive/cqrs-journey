using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Conference.Specflow
{
    static class RegistrationHelper
    {
        public static OrderDTO GetOrder(string email, string accessCode)
        {
            var orderDao = new OrderDao(() => new ConferenceRegistrationDbContext());
            var orderId = orderDao.LocateOrder(email, accessCode).Value;
            return orderDao.GetOrderDetails(orderId);
        }
    }
}
