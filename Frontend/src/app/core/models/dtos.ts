export interface LoginDataDto {
    userId: string; // GUID
    name: string;
    roles: string[];
    accountCreatedAt?: string | null; // ISO
    accessToken: string;
}


export interface LanguageDto {
    id: string;
    code: string;
    displayName: string;
}


export interface UserProblemVersionDto {
    versionId: string;
    problemId: string;
    statement: string;
    totalTests: number;
    supportedLanguages: string[];
}


export interface ProblemDto {
    id: string;
    versionLink?: string | null;
    slug: string;
    title: string;
    isPublished: boolean;
    isDeleted: boolean;
    latestVersion?: UserProblemVersionDto | null;
}

export interface DeletionRequestDto {
    id: string;
    problemId: string;
    initiatorId: string;
    reason: string;
    problemSlug: string;
    problemTitle: string;
    isApproved: boolean;
}

export interface ProblemUpdateDto {
    slug: string;
    title: string;
}

export interface UserDto {
    id: string;
    email: string;
    username: string;
    createdTimestamp: number;
}

export interface UserMetadataDto {
    id: string;
    isDeleted: boolean;
    deletionScheduledAt?: string | null; //iso date string
    deletionDeadline?: string | null; //iso date string
}

export interface RoleDto {
    id: string;
    roleName: string;
}

export interface UpdateRolesDto {
    rolesToRemove?: string[];
    rolesToAdd?: string[];
}

export interface CodeRequestDto {
    requestId: string;
    problemSlug: string;
    problemVersionId: string;
    languageCode: string;
    code: string;
    requestSentAt: string; // ISO
}


export interface ShortSubmissionDto {
    id: string;
    solutionLanguage: string;
    passedTests: number;
    totalTests: number;
    created: string;//iso
}


export interface SubmissionDto {
    submissionId: string;
    versionId: string;
    problemSlug: string;
    title: string;
    solutionLanguage: string;
    solution: string;
    numPassedTests: number;
    totalTests: number;
    cpuTimeUs: number;
    wallTimeMs: number;
    peakMemoryBytes: number;
}


// Editor DTOs (simplified)
export interface AdvancedTest {
    name: string;
    timeoutMs: number;
    source: string;
    languageCode: string;
}

export interface HelpersBlock {
    inline?: string | null;
    languageCode: string;
}

export interface ParameterDescryptor {
    name: string;
    type: TypeDescriptor;
}

export interface SampleTest {
    name: string;
    timeoutMs: number;
    inputs?: any;
    expected?: any;
    comparator: string;
}

export interface Signature {
    returnType: TypeDescriptor;
    parameters: ParameterDescryptor[];
}

export interface TypeDescriptor {
    kind: string;
    name?: string | null;
    items?: TypeDescriptor | null;
    of?: TypeDescriptor | null;
}

export interface ManifestDto {
    entrypoint: string;
    signature: Signature;
    helpers: HelpersBlock[];
    advancedTests: AdvancedTest[];
    sampleTests: SampleTest[];
}

export interface EditorProblemVersionDto {
    versionId: string;
    problemId: string;
    createdAt?: string | null;
    createdBy?: string | null;
    statement: string;
    totalTests: number;
    version: number;
    supportedLanguages: string[];
    testManifest?: ManifestDto | null;
    isPublished: boolean;
}

export enum RequestStatus {
    NoStatus = 0,
    Acknowledged = 1,
    Executing = 2,
    Failed = 3,
    Succeeded = 4,
    Cancelled = 5
}

export enum ExecutionStatus {
    NoStatus = 0,
    Succeeded = 1,
    CompileError = 2,
    RuntimeError = 3,
    TimedOut = 4,
    Cancelled = 5,
    FailedToExecute = 6,
    Pending = 7
}

export interface ExecutionResultDto {
    status: ExecutionStatus;
    exitCode: number;
    consoleOutput?: string | null;
    passedTests: number;
    totalTests: number;
    peakMemoryBytes: number;
    cpuTimeUs: number;
    wallTimeMs: number;
    requestSentAt: string; //iso
    responseSentAt: string; //iso
}

export interface CodeResponseDto {
    requestId: string;
    status: RequestStatus;
    versionId: string;
    userId: string;
    userSolution: string;
    result: ExecutionResultDto;
}