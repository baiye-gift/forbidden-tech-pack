using System;

namespace ForbiddenTechnologyPack.Core {
    public sealed class SafeRemovalReport {
        public int ConvertedObjectCount { get; private set; }
        public int ConvertedObjects { get { return ConvertedObjectCount; } }
        public float ConvertedMassKg { get; private set; }
        public int ReturnedInputCount { get; private set; }
        public int ReturnedInputs { get { return ReturnedInputCount; } }
        public int RemovedBuildingCount { get; private set; }
        public int RemovedBuildings { get { return RemovedBuildingCount; } }
        public int RemainingCustomObjectCount { get; private set; }
        public bool IsComplete { get; private set; }

        public void RecordConversion(float massKg) {
            RecordConvertedObject(massKg);
        }

        public void RecordConvertedObject(float massKg) {
            if (float.IsNaN(massKg) || float.IsInfinity(massKg) || massKg < 0f) {
                throw new ArgumentOutOfRangeException("massKg");
            }
            ConvertedObjectCount++;
            ConvertedMassKg += massKg;
        }

        public void RecordReturnedInput(int count) {
            RecordReturnedInputs(count);
        }

        public void RecordReturnedInputs(int count) {
            if (count < 0) {
                throw new ArgumentOutOfRangeException("count");
            }
            ReturnedInputCount += count;
        }

        public void RecordRemovedBuilding() {
            RemovedBuildingCount++;
        }

        public void Finish(int remainingCustomObjectCount) {
            if (remainingCustomObjectCount < 0) {
                throw new ArgumentOutOfRangeException("remainingCustomObjectCount");
            }
            RemainingCustomObjectCount = remainingCustomObjectCount;
            IsComplete = remainingCustomObjectCount == 0;
        }
    }
}
