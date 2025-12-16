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
status: string; // "Deleted", "no version", or other
reason?: string | null;
latestVersion?: UserProblemVersionDto | null;
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
}


export interface SubmissionDto {
submissionId: string;
versionId: string;
problemSlug: string;
solutionLanguage: string;
solution: string;
numPassedTests: number;
totalTests: number;
}


// Editor DTOs (simplified)
export interface EditorProblemVersionDto {
problemId: string;
createdAt?: string | null;
createdBy?: string | null;
statement: string;
totalTests: number;
version: number;
supportedLanguages: string[];
testManifest?: any | null;
}