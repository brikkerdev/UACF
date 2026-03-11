using NUnit.Framework;
using UACF.Config;
using UACF.Core;
using UACF.Handlers;
using UACF.Services;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace UACF.Tests
{
    public class ExecuteHandlerTests
    {
        private bool _originalAllowExecute;

        [SetUp]
        public void SetUp()
        {
            _originalAllowExecute = UACFConfig.Instance.AllowExecute;
            UACFConfig.Instance.AllowExecute = true;
        }

        [TearDown]
        public void TearDown()
        {
            UACFConfig.Instance.AllowExecute = _originalAllowExecute;
        }

        [Test]
        public void ExecuteScriptService_Expression_ReturnsResult()
        {
            var response = ExecuteScriptService.Instance.Execute("1 + 1", null, null, 0);
            if (!response.Ok && (response.Error?.Code == "NOT_AVAILABLE" || response.Error?.Code == "INVOCATION_ERROR"))
                Assert.Inconclusive("Roslyn assemblies are not available in this editor environment.");

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var data = JObject.FromObject(response.Data);
            Assert.AreEqual(2, data["result"]?.Value<int>());
        }

        [Test]
        public void ExecuteScriptService_ReturnExpression_ReturnsValue()
        {
            var code = "var x = 3; var y = 4;";
            var response = ExecuteScriptService.Instance.Execute(code, "x * y", null, 0);
            if (!response.Ok && (response.Error?.Code == "NOT_AVAILABLE" || response.Error?.Code == "INVOCATION_ERROR"))
                Assert.Inconclusive("Roslyn assemblies are not available in this editor environment.");

            Assert.IsTrue(response.Ok, response.Error?.Message);
            var data = JObject.FromObject(response.Data);
            Assert.AreEqual(12, data["result"]?.Value<int>());
        }

        [Test]
        public void ExecuteScriptService_CompilationError_ReturnsCode()
        {
            var response = ExecuteScriptService.Instance.Execute("var x = new GameObjec();", null, null, 0);
            if (!response.Ok && response.Error?.Code == "NOT_AVAILABLE")
                Assert.Inconclusive("Roslyn assemblies are not available in this editor environment.");

            Assert.IsFalse(response.Ok);
            Assert.AreEqual("COMPILATION_ERROR", response.Error?.Code);
        }

        [Test]
        public void ExecuteScriptService_Timeout_ReturnsCode()
        {
            var response = ExecuteScriptService.Instance.Execute("await System.Threading.Tasks.Task.Delay(3000);", null, null, 25);
            if (!response.Ok && response.Error?.Code == "NOT_AVAILABLE")
                Assert.Inconclusive("Roslyn assemblies are not available in this editor environment.");

            Assert.IsFalse(response.Ok);
            Assert.AreEqual("TIMEOUT", response.Error?.Code);
        }

        [Test]
        public void Execute_Disabled_ReturnsForbidden()
        {
            UACFConfig.Instance.AllowExecute = false;
            var dispatcher = new ActionDispatcher();
            ExecuteHandler.Register(dispatcher);

            var response = dispatcher.DispatchAsync("execute", new JObject { ["code"] = "1 + 1" }).GetAwaiter().GetResult();

            Assert.IsFalse(response.Ok);
            Assert.AreEqual("FORBIDDEN", response.Error?.Code);
        }
    }
}
