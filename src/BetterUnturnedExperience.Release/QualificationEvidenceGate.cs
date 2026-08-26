using System;

namespace BetterUnturnedExperience.Release
{
    public enum QualificationEvidenceGateStatus : byte
    {
        InvalidPolicy,
        EvidencePackageInvalid,
        QualificationIncomplete,
        TechnicallyQualified
    }

    public sealed class QualificationEvidenceGateResult
    {
        internal QualificationEvidenceGateResult(QualificationEvidenceGateStatus status, EvidencePackageValidationResult packageValidation, QualificationResult qualification, CandidateBuildDescriptor candidate)
        {
            Status = status;
            PackageValidation = packageValidation;
            Qualification = qualification;
            BuildIdentity = candidate == null ? null : candidate.BuildIdentity;
            DllSha256 = candidate == null ? null : candidate.DllSha256;
        }

        public QualificationEvidenceGateStatus Status { get; }
        public EvidencePackageValidationResult PackageValidation { get; }
        public QualificationResult Qualification { get; }
        public string BuildIdentity { get; }
        public string DllSha256 { get; }
        public bool IsTechnicallyQualified { get { return Status == QualificationEvidenceGateStatus.TechnicallyQualified; } }
    }

    public static class QualificationEvidenceGate
    {
        private static readonly EvidenceEnvironmentRole[] Roles =
        {
            EvidenceEnvironmentRole.SinglePlayer,
            EvidenceEnvironmentRole.SteamP2PHost,
            EvidenceEnvironmentRole.SteamP2PClient,
            EvidenceEnvironmentRole.U3dsHeadless,
            EvidenceEnvironmentRole.U3dsClientUi
        };

        public static QualificationEvidenceGateResult Evaluate(RuntimeEvidencePackage package, CandidateBuildDescriptor candidate, QualificationPolicy policy)
        {
            var packageValidation = RuntimeEvidencePackageValidator.Validate(package, candidate);
            if (policy == null)
                return new QualificationEvidenceGateResult(QualificationEvidenceGateStatus.InvalidPolicy, packageValidation, null, candidate);
            if (!packageValidation.IsValid)
                return new QualificationEvidenceGateResult(QualificationEvidenceGateStatus.EvidencePackageInvalid, packageValidation, null, candidate);

            var qualification = QualificationEvaluator.Evaluate(package.Cases, candidate, policy);
            var status = AllApplicableRolesFulfilled(qualification) ? QualificationEvidenceGateStatus.TechnicallyQualified : QualificationEvidenceGateStatus.QualificationIncomplete;
            return new QualificationEvidenceGateResult(status, packageValidation, qualification, candidate);
        }

        private static bool AllApplicableRolesFulfilled(QualificationResult qualification)
        {
            foreach (var role in Roles)
            {
                var verdict = qualification.For(role);
                if (verdict != QualificationVerdict.Fulfilled && verdict != QualificationVerdict.NotApplicable)
                    return false;
            }
            return true;
        }
    }
}
