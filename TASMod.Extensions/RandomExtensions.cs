// uses the prng cloning for Net5 randoms defined in this stackoverflow post:
// https://stackoverflow.com/questions/8188844/is-there-a-way-to-grab-the-actual-state-of-system-random
// for some reason the way I was doing it manually just did not work at all...
// probably because we have no type information on the internal state of the random object for reflector to churn on

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TASMod.Extensions
{
    public static class RandomExtensions
    {
        public static int SharedSeed = 0;
        public static Random SharedRandom;
        public static bool UseSharedRandom = true;
        public static Dictionary<int, List<string>> StackTraces;

        public const string ImplName = "_impl";
        public const string Net6_ImplName = "XoshiroImpl";
        public const string Net6_ImplTypeName = "System.Random+XoshiroImpl";
        public const string Net6_S0Name = "_s0";
        public const string Net6_S1Name = "_s1";
        public const string Net6_S2Name = "_s2";
        public const string Net6_S3Name = "_s3";
        private static FieldInfo S0Info;
        private static FieldInfo S1Info;
        private static FieldInfo S2Info;
        private static FieldInfo S3Info;

        public const string Net5_ImplName = "Net5CompatSeedImpl";
        public const string Net5_ImplTypeName = "System.Random+Net5CompatSeedImpl";
        public const string Net5_CompatPrngTypeName = "System.Random+CompatPrng";
        public const string Net5_Impl_CompatPrng = "_prng";
        public const string CompatPrng_SeedArrayName = "_seedArray";
        public const string CompatPrng_InextName = "_inext";
        public const string CompatPrng_InextpName = "_inextp";
        private static FieldInfo ImplInfo;
        private static FieldInfo PrngInfo;
        private static FieldInfo seedArrayInfo;
        private static FieldInfo inextInfo;
        private static FieldInfo inextpInfo;

        internal class Holder
        {
            public int Index = 0;
            public int Seed = 0;
            public bool IsNet6 = false;
        }

        internal static ConditionalWeakTable<Random, Holder> RandomData;

        static RandomExtensions()
        {
            RandomData = new ConditionalWeakTable<Random, Holder>();
            ImplInfo = typeof(Random).GetField(
                ImplName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            PrngInfo = Type.GetType(Net5_ImplTypeName)!
                .GetField(Net5_Impl_CompatPrng, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Type compatPrngType = Type.GetType(Net5_CompatPrngTypeName)!;
            seedArrayInfo = compatPrngType.GetField(
                CompatPrng_SeedArrayName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            inextInfo = compatPrngType.GetField(
                CompatPrng_InextName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            inextpInfo = compatPrngType.GetField(
                CompatPrng_InextpName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

            Type xoroshiroType = Type.GetType(Net6_ImplTypeName)!;
            S0Info = xoroshiroType.GetField(
                Net6_S0Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            S1Info = xoroshiroType.GetField(
                Net6_S1Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            S2Info = xoroshiroType.GetField(
                Net6_S2Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            S3Info = xoroshiroType.GetField(
                Net6_S3Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

            SharedRandom = new Random(SharedSeed);
            StackTraces = new Dictionary<int, List<string>>();
        }

        public static void Reset()
        {
            SharedRandom = new Random(SharedSeed);
            StackTraces.Clear();
        }

        public static void Update()
        {
            SharedRandom.Next();
        }

        public static void InitData(this Random random)
        {
            var data = RandomData.GetOrCreateValue(random);
            data.IsNet6 = true;
            data.Seed = 0;
            data.Index = 0;
        }

        public static void InitData(this Random random, int seed)
        {
            var data = RandomData.GetOrCreateValue(random);
            data.IsNet6 = false;
            data.Seed = seed;
            data.Index = 0;
        }

        public static bool IsNet6(this Random random)
        {
            return random.GetImpl().GetType().Name == Net6_ImplName;
        }

        //Random > Impl > CompatPrng
        public static object GetImpl(this Random random) =>
            ImplInfo.GetValueDirect(__makeref(random))!;

        public static object GetCompatPrng(object impl) =>
            PrngInfo.GetValueDirect(__makeref(impl))!;

        public static object GetCompatPrng(this Random random)
        {
            if (random.IsNet6())
            {
                throw new Exception("random types do not match");
            }
            object impl = GetImpl(random);
            return PrngInfo.GetValueDirect(__makeref(impl))!;
        }

        // allows copying of random objects
        public static Random Copy(this Random random)
        {
            Random other;
            if (random.IsNet6())
            {
                UseSharedRandom = false;
                other = new Random();
                CloneNet6(random, other);
                UseSharedRandom = true;
                return other;
            }
            else
            {
                other = new Random(0);
                CloneNet5(random, other);
                return other;
            }
        }

        public static int Peek(this Random random)
        {
            Random r = random.Copy();
            return r.Next();
        }

        public static double PeekDouble(this Random random)
        {
            Random r = random.Copy();
            return r.NextDouble();
        }

        private static void CloneNet5(this Random random, Random other)
        {
            if (other.IsNet6())
            {
                throw new Exception("random types do not match");
            }

            object otherImpl = other.GetImpl();
            TypedReference otherImplRef = __makeref(otherImpl);
            object otherPrng = PrngInfo.GetValueDirect(otherImplRef)!;

            object currImpl = random.GetImpl();
            TypedReference currImplRef = __makeref(currImpl);
            object currPrng = PrngInfo.GetValueDirect(currImplRef)!;

            seedArrayInfo.SetValue(otherPrng, ((int[])seedArrayInfo.GetValue(currPrng)).Clone());
            inextInfo.SetValue(otherPrng, inextInfo.GetValue(currPrng));
            inextpInfo.SetValue(otherPrng, inextpInfo.GetValue(currPrng));
            PrngInfo.SetValueDirect(otherImplRef, otherPrng);

            other.set_Index(random.get_Index());
            other.set_Seed(random.get_Seed());
        }

        private static void CloneNet6(this Random random, Random other)
        {
            if (!other.IsNet6())
            {
                throw new Exception("random types do not match");
            }
            object oldImpl = random.GetImpl();
            object newImpl = other.GetImpl();

            S0Info.SetValue(newImpl, S0Info.GetValue(oldImpl));
            S1Info.SetValue(newImpl, S1Info.GetValue(oldImpl));
            S2Info.SetValue(newImpl, S2Info.GetValue(oldImpl));
            S3Info.SetValue(newImpl, S3Info.GetValue(oldImpl));

            other.set_Index(random.get_Index());
            other.set_Seed(random.get_Seed());
        }

        public static void CloneOver(this Random random, Random other)
        {
            if (random.IsNet6())
            {
                CloneNet6(random, other);
            }
            else
            {
                CloneNet5(random, other);
            }
        }

        public static int IncrementCounter(this Random random)
        {
            Holder data = RandomData.GetOrCreateValue(random);
            return ++data.Index;
        }

        public static int IncrementCounter(this Random random, int n)
        {
            Holder data = RandomData.GetOrCreateValue(random);
            data.Index += n;
            return data.Index;
        }

        public static void set_Index(this Random random, int index)
        {
            Holder data = RandomData.GetOrCreateValue(random);
            data.Index = index;
        }

        public static int get_Index(this Random random)
        {
            Holder data = RandomData.GetOrCreateValue(random);
            return data.Index;
        }

        public static void set_Seed(this Random random, int seed)
        {
            Holder data = RandomData.GetOrCreateValue(random);
            data.Seed = seed;
        }

        public static int get_Seed(this Random random)
        {
            Holder data = RandomData.GetOrCreateValue(random);
            return data.Seed;
        }

        public static ulong get_S0(this Random random)
        {
            return (ulong)S0Info.GetValue(random.GetImpl())!;
        }

        public static void set_S0(this Random random, ulong value)
        {
            S0Info.SetValue(random.GetImpl(), value);
        }

        public static ulong get_S1(this Random random)
        {
            return (ulong)S1Info.GetValue(random.GetImpl())!;
        }

        public static void set_S1(this Random random, ulong value)
        {
            S1Info.SetValue(random.GetImpl(), value);
        }

        public static ulong get_S2(this Random random)
        {
            return (ulong)S2Info.GetValue(random.GetImpl())!;
        }

        public static void set_S2(this Random random, ulong value)
        {
            S2Info.SetValue(random.GetImpl(), value);
        }

        public static ulong get_S3(this Random random)
        {
            return (ulong)S3Info.GetValue(random.GetImpl())!;
        }

        public static void set_S3(this Random random, ulong value)
        {
            S3Info.SetValue(random.GetImpl(), value);
        }

        public static unsafe void LoadFromShared(this Random random)
        {
            if (!random.IsNet6())
            {
                throw new Exception("random types do not match");
            }

            if (!UseSharedRandom)
                return;

            ulong s0 = 0,
                s1 = 0,
                s2 = 0,
                s3 = 0;
            while ((s0 | s1 | s2 | s3) == 0)
            {
                s0 = SharedRandom.Net5NextUInt64();
                s1 = SharedRandom.Net5NextUInt64();
                s2 = SharedRandom.Net5NextUInt64();
                s3 = SharedRandom.Net5NextUInt64();
            }
            random.set_S0(s0);
            random.set_S1(s1);
            random.set_S2(s2);
            random.set_S3(s3);
        }

        public static ulong Net5NextUInt64(this Random random)
        {
            return (uint)random.Next(4194304)
                | ((ulong)(uint)random.Next(4194304) << 22)
                | ((ulong)(uint)random.Next(1048576) << 44);
        }
    }
}
