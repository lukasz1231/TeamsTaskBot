using Domain.Interfaces;
using Domain.Entities.Enums;
namespace Infrastructure.Services
{
    public class ActionHandlerFactory
    {
        private readonly IEnumerable<IActionHandlerStrategy> _handlers;

        public ActionHandlerFactory(IEnumerable<IActionHandlerStrategy> handlers)
        {
            _handlers = handlers;
        }

        public IActionHandlerStrategy GetHandler(ActionType actionType)
        {
            var handler = _handlers.FirstOrDefault(h => h.ActionType == actionType);

            if (handler == null)
            {
                throw new InvalidOperationException($"No handler found for action type: {actionType}");
            }

            return handler;
        }
    }
}