using System.Runtime.CompilerServices;
using Unity.Burst;

[assembly: InternalsVisibleTo("CodeSmile.Tests.GMesh")]
[assembly: InternalsVisibleTo("CodeSmile.Tests.GMesh.Editor")]
[assembly: BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low,
	DisableSafetyChecks = false)]