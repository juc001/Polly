namespace Polly.RetryPolicy.Test
{
    public class RetryPolicyUnitTest
    {
        [Fact]
        public void RetryPolicy_Should_ExecuteAction_WithoutRetry_OnSuccess()
        {
            // 設定參數
            bool actionExecuted = false;

            //_,_,_=exception, timespan, retryCount異常,時間差,幾次重試
            var retryPolicy = Policy
                .Handle<Exception>()
                .Retry(3, (_, _, _) => { });

            // 執行API完成
            retryPolicy.Execute(() =>
            {
                actionExecuted = true;
            });

            // 驗證
            Assert.True(actionExecuted, "Action should be executed.");
        }
        [Fact]
        public void RetryPolicy_Should_Retry_WithExponentialBackoff_OnTransientFailure()
        {
            // 定義參數
            int retryCount = 0;
            List<TimeSpan> retryDelays = new List<TimeSpan>();

            var retryPolicy = Policy
                .Handle<TimeoutException>()
                .WaitAndRetry(3, // 最多3次
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 指數回退策略
                    (_, timeSpan, currentRetryCount, _) =>
                    {
                        retryCount = currentRetryCount;
                        retryDelays.Add(timeSpan);
                    });

            // 執行
            Assert.Throws<TimeoutException>(() =>
            {
                retryPolicy.Execute(() =>
                {
                    throw new TimeoutException("Temporary failure");
                });
            });

            // 驗證
            Assert.Equal(3, retryCount); // 應該重試3次
            Assert.Collection(retryDelays,
                delay => Assert.Equal(TimeSpan.FromSeconds(2), delay), // 第一次2秒
                delay => Assert.Equal(TimeSpan.FromSeconds(4), delay), // 第二次4秒
                delay => Assert.Equal(TimeSpan.FromSeconds(8), delay)  // 第三次8秒
            );
        }
        [Fact]
        public void RetryPolicy_Should_ExecuteFunction_WithoutRetry_OnSuccess()
        {
            // 定義參數
            var retryPolicy = Policy<string>
                .Handle<Exception>()
                .Retry(3, (_, _, _) => { });

            // 執行
            var result = retryPolicy.Execute(() =>
            {
                return "Success";
            });

            // 驗證
            Assert.Equal("Success", result); 
        }
        [Fact]
        public void RetryPolicy_Should_Retry_WithExponentialBackoff_OnTransientFailure_WithResult()
        {
            // 定義參數
            int retryCount = 0;
            List<TimeSpan> retryDelays = new List<TimeSpan>();

            var retryPolicy = Policy<string>
                .Handle<TimeoutException>()
                .WaitAndRetry(3, // 最多3次
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 指數回退策略
                    (_, timeSpan, currentRetryCount, _) =>
                    {
                        retryCount = currentRetryCount; 
                        retryDelays.Add(timeSpan); 
                    });

            // 執行模擬前兩次失敗最後成功
            var result = retryPolicy.Execute(() =>
            {
                if (retryCount < 3)
                {
                    throw new TimeoutException("Temporary failure");
                }
                return "Success";
            });

            // 驗證
            Assert.Equal(3, retryCount); // 驗證重複次數
            Assert.Collection(retryDelays,
                delay => Assert.Equal(TimeSpan.FromSeconds(2), delay), // 第一次2秒
                delay => Assert.Equal(TimeSpan.FromSeconds(4), delay), // 第二次4秒
                delay => Assert.Equal(TimeSpan.FromSeconds(8), delay)  // 第三次8秒
            );
            Assert.Equal("Success", result); 
        }

    }
}
