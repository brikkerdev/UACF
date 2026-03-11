using System.Linq;
using NUnit.Framework;
using UACF.Core;

namespace UACF.Tests
{
    public class ServerTests
    {
        [Test]
        public void RequestRouter_AcceptsUacfEndpoint()
        {
            var dispatcher = new ActionDispatcher();
            var handler = new UacfEndpointHandler(dispatcher);
            var router = new RequestRouter(handler);
            Assert.IsNotNull(router);
        }

        [Test]
        public void ActionsRegistry_HasApiActions()
        {
            var actions = ActionsRegistry.All;
            Assert.IsNotNull(actions);
            Assert.Greater(actions.Count, 0);
            Assert.IsTrue(actions.Any(a => a.Action == "api.list"));
        }

        [Test]
        public void UacfResponse_SuccessFormat()
        {
            var r = UACF.Models.UacfResponse.Success(new { test = 1 }, 0.5);
            Assert.IsTrue(r.Ok);
            Assert.IsNotNull(r.Data);
            Assert.AreEqual(0.5, r.Duration);
        }

        [Test]
        public void UacfResponse_FailFormat()
        {
            var r = UACF.Models.UacfResponse.Fail("TEST", "message", "suggestion", 0.1);
            Assert.IsFalse(r.Ok);
            Assert.IsNotNull(r.Error);
            Assert.AreEqual("TEST", r.Error.Code);
            Assert.AreEqual("message", r.Error.Message);
            Assert.AreEqual("suggestion", r.Error.Suggestion);
        }
    }
}
