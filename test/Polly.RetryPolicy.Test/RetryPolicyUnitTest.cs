namespace Polly.RetryPolicy.Test
{
    public class RetryPolicyUnitTest
    {
        [Fact]
        public void RetryPolicy_Should_ExecuteAction_WithoutRetry_OnSuccess()
        {
            // �]�w�Ѽ�
            bool actionExecuted = false;

            //_,_,_=exception, timespan, retryCount���`,�ɶ��t,�X������
            var retryPolicy = Policy
                .Handle<Exception>()
                .Retry(3, (_, _, _) => { });

            // ����API����
            retryPolicy.Execute(() =>
            {
                actionExecuted = true;
            });

            // ����
            Assert.True(actionExecuted, "Action should be executed.");
        }
        [Fact]
        public void RetryPolicy_Should_Retry_WithExponentialBackoff_OnTransientFailure()
        {
            // �w�q�Ѽ�
            int retryCount = 0;
            List<TimeSpan> retryDelays = new List<TimeSpan>();

            var retryPolicy = Policy
                .Handle<TimeoutException>()
                .WaitAndRetry(3, // �̦h3��
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // ���Ʀ^�h����
                    (_, timeSpan, currentRetryCount, _) =>
                    {
                        retryCount = currentRetryCount;
                        retryDelays.Add(timeSpan);
                    });

            // ����
            Assert.Throws<TimeoutException>(() =>
            {
                retryPolicy.Execute(() =>
                {
                    throw new TimeoutException("Temporary failure");
                });
            });

            // ����
            Assert.Equal(3, retryCount); // ���ӭ���3��
            Assert.Collection(retryDelays,
                delay => Assert.Equal(TimeSpan.FromSeconds(2), delay), // �Ĥ@��2��
                delay => Assert.Equal(TimeSpan.FromSeconds(4), delay), // �ĤG��4��
                delay => Assert.Equal(TimeSpan.FromSeconds(8), delay)  // �ĤT��8��
            );
        }
        [Fact]
        public void RetryPolicy_Should_ExecuteFunction_WithoutRetry_OnSuccess()
        {
            // �w�q�Ѽ�
            var retryPolicy = Policy<string>
                .Handle<Exception>()
                .Retry(3, (_, _, _) => { });

            // ����
            var result = retryPolicy.Execute(() =>
            {
                return "Success";
            });

            // ����
            Assert.Equal("Success", result); 
        }
        [Fact]
        public void RetryPolicy_Should_Retry_WithExponentialBackoff_OnTransientFailure_WithResult()
        {
            // �w�q�Ѽ�
            int retryCount = 0;
            List<TimeSpan> retryDelays = new List<TimeSpan>();

            var retryPolicy = Policy<string>
                .Handle<TimeoutException>()
                .WaitAndRetry(3, // �̦h3��
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // ���Ʀ^�h����
                    (_, timeSpan, currentRetryCount, _) =>
                    {
                        retryCount = currentRetryCount; 
                        retryDelays.Add(timeSpan); 
                    });

            // ��������e�⦸���ѳ̫ᦨ�\
            var result = retryPolicy.Execute(() =>
            {
                if (retryCount < 3)
                {
                    throw new TimeoutException("Temporary failure");
                }
                return "Success";
            });

            // ����
            Assert.Equal(3, retryCount); // ���ҭ��Ʀ���
            Assert.Collection(retryDelays,
                delay => Assert.Equal(TimeSpan.FromSeconds(2), delay), // �Ĥ@��2��
                delay => Assert.Equal(TimeSpan.FromSeconds(4), delay), // �ĤG��4��
                delay => Assert.Equal(TimeSpan.FromSeconds(8), delay)  // �ĤT��8��
            );
            Assert.Equal("Success", result); 
        }

    }
}
