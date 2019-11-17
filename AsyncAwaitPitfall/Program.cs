using System;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;
using static AsyncAwaitPitfall.ErrorManagmentUtils;

namespace AsyncAwaitPitfall
{
    static class ErrorManagmentUtils
    {
        public static T Silently<T>(Func<T> expression, T fallback)
        {
            try
            {
                return expression();
            }
            catch (Exception)
            {
                Console.WriteLine("Ignoring exception.");
            }

            return fallback;
        }

        public static void Silently(Action statement)
        {
            try
            {
                statement();
            }
            catch (Exception)
            {
                Console.WriteLine("Ignoring exception.");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Demo(); // Fire and forget
            Thread.Sleep(1000); // gives time to async operations to finish
        }

        public static async void Demo()
        {
            #region Building blocks

            int TwoMayCrash(bool crash = true)
            {
                if (crash)
                    throw new InvalidOperationException();
                return 2;
            }

            int Two() => TwoMayCrash(false);
            int TwoCrash() => TwoMayCrash();

            async Task<int> TwoAsync()
            {
                return await Task.Run(Two);
            }

            async Task<int> TwoCrashAsync()
            {
                return await Task.Run(TwoCrash);
            }

            Action Effect = () => Console.WriteLine("Computed effect");
            Action EffectCrash = () => throw new InvalidOperationException();

            async Task EffectAsync()
            {
                await Task.Run(Effect);
            }

            async Task EffectCrashAsync()
            {
                await Task.Run(EffectCrash);
            }

            #endregion

            #region Meat

            try
            {
                Console.WriteLine("============ Simple expressions.");
                Assert(Silently(Two, 0) == 2, "Most basic example, should return 2");
                Assert(Silently(TwoCrash, 0) == 0, "Here, the expression fails, we fallback.");
                Console.WriteLine("============");

                Console.WriteLine("============ Simple effects.");
                Silently(Effect); // Print that effect took place
                Silently(EffectCrash); // Print that effect failed, silently ignore the exception
                Console.WriteLine("============");


                Console.WriteLine(
                    "============ Welcome to async world. ( ͡° ͜ʖ ͡°).");

                //
                // This seems correct, right ? our Effect is run. You can check it on the console.
                // In case that async computation failure to launch, we get a completed task instead.
                // This is neat and easy to use !
                // 
                await Silently(EffectAsync, Task.CompletedTask);
                int two = await Silently(TwoAsync, Task.FromResult(0));
                
                //
                // Here is what smells bad, We expect that everything wrapped in Silently never poses any problem
                // no exception should happen, *right* ?  You will be disappointed
                //
                // await Silently(EffectCrashAsync, Task.CompletedTask); // TODO: Uncomment me to see the world collapse !
                // await Silently(TwoCrashAsync, Task.FromResult(0)); // TODO: Uncomment me to see the world collapse !

                // await SilentlyEffectAsync(); TODO: Uncomment me to see the world collapse

                Console.WriteLine("============");
            }
            catch (Exception e)
            {
                // As proud and clever developers, we are really confident this is not even needed. Everything is silenced !
                Console.WriteLine("Oh no, this should not happen, an exception escaped !!!");
            }

            #endregion
        }

        /// <summary>
        /// Represent the same pitfall wrote in a different manner
        /// </summary>
        public static Task SilentlyEffectAsync()
        {
            try
            {
                return Task.Run(() => throw new InvalidOperationException());
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignoring exception.");
            }

            return Task.CompletedTask;
        }
    }
}
