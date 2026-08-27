using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class XRNearFarReticleSafetyHarness
{
    const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

    [MenuItem("Tools/XR/Validate Near-Far Reticle Safety")]
    public static void Validate()
    {
        Type visualType = typeof(XRNearFarReticleVisual);
        MethodInfo vectorIsFinite = visualType.GetMethod(
            "IsFinite",
            StaticPrivate,
            null,
            new[] { typeof(Vector3) },
            null);
        MethodInfo quaternionIsFinite = visualType.GetMethod(
            "IsFinite",
            StaticPrivate,
            null,
            new[] { typeof(Quaternion) },
            null);
        MethodInfo tryNormalize = visualType.GetMethod(
            "TryNormalize",
            StaticPrivate,
            null,
            new[] { typeof(Vector3), typeof(Vector3).MakeByRefType() },
            null);
        MethodInfo linePointUsable = visualType.GetMethod(
            "IsWorldLinePointUsable",
            StaticPrivate,
            null,
            new[] { typeof(Vector3), typeof(Vector3), typeof(float) },
            null);

        if (vectorIsFinite == null
            || quaternionIsFinite == null
            || tryNormalize == null
            || linePointUsable == null)
        {
            Fail("Required finite-value guards were not found on XRNearFarReticleVisual.");
        }

        if (!InvokePredicate(vectorIsFinite, Vector3.zero))
            Fail("A valid Vector3 was rejected.");
        if (InvokePredicate(vectorIsFinite, new Vector3(float.NaN, 0f, 0f)))
            Fail("A Vector3 containing NaN was accepted.");
        if (InvokePredicate(vectorIsFinite, new Vector3(0f, float.PositiveInfinity, 0f)))
            Fail("A Vector3 containing Infinity was accepted.");
        if (!InvokePredicate(quaternionIsFinite, Quaternion.identity))
            Fail("A valid Quaternion was rejected.");
        if (InvokePredicate(quaternionIsFinite, new Quaternion(0f, 0f, float.NaN, 1f)))
            Fail("A Quaternion containing NaN was accepted.");

        object[] validNormalizeArgs = { new Vector3(0f, 3f, 0f), Vector3.zero };
        if (!(bool)tryNormalize.Invoke(null, validNormalizeArgs)
            || !Approximately((Vector3)validNormalizeArgs[1], Vector3.up))
        {
            Fail("A valid direction could not be normalized safely.");
        }

        object[] invalidNormalizeArgs = { new Vector3(float.NaN, 0f, 0f), Vector3.zero };
        if ((bool)tryNormalize.Invoke(null, invalidNormalizeArgs))
            Fail("A direction containing NaN was normalized instead of being rejected.");

        if (!(bool)linePointUsable.Invoke(
                null,
                new object[] { new Vector3(1f, 0f, 0f), Vector3.zero, 4f }))
        {
            Fail("A valid LineRenderer point inside the safety distance was rejected.");
        }

        if ((bool)linePointUsable.Invoke(
                null,
                new object[] { new Vector3(float.NaN, 0f, 0f), Vector3.zero, 4f }))
        {
            Fail("A LineRenderer point containing NaN was accepted.");
        }

        if ((bool)linePointUsable.Invoke(
                null,
                new object[] { new Vector3(3f, 0f, 0f), Vector3.zero, 4f }))
        {
            Fail("A LineRenderer point beyond the safety distance was accepted.");
        }

        Debug.Log(
            "XR near-far visual safety validation passed: finite reticle data and nearby "
            + "line points are accepted, while NaN/Infinity and over-distance values are rejected.");
    }

    static bool InvokePredicate(MethodInfo method, object value)
    {
        return (bool)method.Invoke(null, new[] { value });
    }

    static bool Approximately(Vector3 lhs, Vector3 rhs)
    {
        return (lhs - rhs).sqrMagnitude < 0.000001f;
    }

    static void Fail(string message)
    {
        string fullMessage = $"XR near-far reticle safety validation failed: {message}";
        Debug.LogError(fullMessage);
        throw new InvalidOperationException(fullMessage);
    }
}
