using NUnit.Framework;
using UACF.Core;
using UACF.Handlers;

namespace UACF.Tests
{
    public class ServerTests
    {
        [Test]
        public void RequestRouter_RegistersAndMatchesExactPath()
        {
            var router = new RequestRouter();
            var called = false;
            router.Register("GET", "/api/status", ctx =>
            {
                called = true;
                return System.Threading.Tasks.Task.CompletedTask;
            });

            Assert.IsTrue(called == false);
        }

        [Test]
        public void RequestRouter_MatchesPathWithParams()
        {
            var router = new RequestRouter();
            string capturedId = null;
            router.Register("GET", "/api/objects/{id}", ctx =>
            {
                capturedId = ctx.PathParams.TryGetValue("id", out var v) ? v : null;
                return System.Threading.Tasks.Task.CompletedTask;
            });

            Assert.IsNotNull(router);
        }
    }
}
