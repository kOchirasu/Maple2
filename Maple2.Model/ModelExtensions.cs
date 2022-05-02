using Maple2.Model.Enum;

namespace Maple2.Model;

public static class ModelExtensions {
    public static JobCode Code(this Job job) {
        return (JobCode) ((int) job / 10);
    }
}
