import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environment';
import { LanguageDto, ProblemDto, EditorProblemVersionDto, ShortSubmissionDto, SubmissionDto, UserProblemVersionDto, DeletionRequestDto, ProblemUpdateDto, UserDto, RoleDto, UpdateRolesDto, UserMetadataDto } from '../models/dtos';


@Injectable({ providedIn: 'root' })
export class ApiService {
    constructor(private http: HttpClient) { }


    getLanguages() {
        return this.http.get<LanguageDto[]>(`${environment.apiBaseUrl}/languages`);
    }


    getProblems(
        {
            getDeleted = null,
            includeLatestVersion = null,
            includeLanguages = null
        }: {
            getDeleted?: boolean | null;
            includeLatestVersion?: boolean | null;
            includeLanguages?: boolean | null;
        } = {}) {
        let params = new HttpParams()

        if (getDeleted != null)
            params = params.set('getDeleted', getDeleted);
        if (includeLatestVersion != null)
            params = params.set('includeLatestVersion', includeLatestVersion);
        if (includeLanguages != null)
            params = params.set('includeLanguages', includeLanguages);

        return this.http.get<ProblemDto[]>(`${environment.apiBaseUrl}/problems`, { params });
    }


    getEditorVersion(slug: string) {
        return this.http.get<EditorProblemVersionDto>(`${environment.apiBaseUrl}/problems/${slug}/latest-version`);
    }

    getUserVersion(id: string) {
        return this.http.get<UserProblemVersionDto>(`${environment.apiBaseUrl}/versions/${id}`);
    }

    getVersionAsEditor(id: string) {
        return this.http.get<EditorProblemVersionDto>(`${environment.apiBaseUrl}/versions/${id}/as-editor`);
    }

    saveProblemVersion(problemId: string, dto: any) {
        return this.http.post(`${environment.apiBaseUrl}/problems/${problemId}/versions`, dto);
    }

    updateVersionDraft(problemId: string, draftId: string, dto: any) {
        return this.http.patch(`${environment.apiBaseUrl}/problems/${problemId}/versions/${draftId}`, dto);
    }

    getVersions() {
        return this.http.get<EditorProblemVersionDto[]>(`${environment.apiBaseUrl}/versions`);
    }


    publishVersion(versionId: string) {
        return this.http.patch(`${environment.apiBaseUrl}/versions/${versionId}`, {});
    }


    deleteVersion(problemId: string, versionId: string) {
        return this.http.delete(`${environment.apiBaseUrl}/problems/${problemId}/versions/${versionId}`);
    }


    getSubmissions(userId: string) {
        return this.http.get<ShortSubmissionDto[]>(`${environment.apiBaseUrl}/user/${userId}/submissions`);
    }


    getSubmission(userId: string, submissionId: string) {
        return this.http.get<SubmissionDto>(`${environment.apiBaseUrl}/user/${userId}/submissions/${submissionId}`);
    }


    deleteSubmission(userId: string, submissionId: string) {
        return this.http.delete(`${environment.apiBaseUrl}/user/${userId}/submissions/${submissionId}`);
    }


    submitCode(dto: any) {
        return this.http.post(`${environment.apiBaseUrl}/jobs/start`, dto);
    }

    syncExternalAccount(userId: string) {
        return this.http.post(`${environment.apiBaseUrl}/users`, JSON.stringify(userId),
            {
                headers: {
                    'Content-Type': 'application/json',
                },
            });
    }

    createProblemDeletionRequest(problemId: string, initiatorId: string, reason: string) {
        return this.http.post(`${environment.apiBaseUrl}/problems/${problemId}/deletion-requests`,
            {
                initiatorId: initiatorId,
                reason: reason
            }
        )
    }

    getOwnProblemDeletionRequests(userId: string) {
        return this.http.get<DeletionRequestDto[]>(`${environment.apiBaseUrl}/users/${userId}/deletion-requests`);
    }

    getFilteredProblemDeletionRequests
        (
            {
                excludeUser = null,
                userId = null,
                exactMatch = null,
                problemId = null,
                onlyNotApproved = null
            }: {
                excludeUser?: boolean | null,
                userId?: string | null,
                exactMatch?: boolean | null,
                problemId?: string | null,
                onlyNotApproved?: boolean | null
            } = {}
        ) {
        let params = new HttpParams();

        if (excludeUser != null && userId != null) {
            params = params.set('excludeUser', excludeUser);
            params = params.set('userId', userId);
        }

        if (exactMatch != null && problemId != null) {
            params = params.set('exactMatch', exactMatch);
            params = params.set('problemId', problemId);
        }

        if (onlyNotApproved != null) {
            params = params.set('onlyNotApproved', onlyNotApproved)
        }

        return this.http.get<DeletionRequestDto[]>(`${environment.apiBaseUrl}/deletion-requests`, { params });
    }

    cancelProblemDeletionRequest(problemId: string, requestId: string) {
        return this.http.delete(`${environment.apiBaseUrl}/problems/${problemId}/deletion-requests/${requestId}`);
    }

    approveProblemDeletionRequest(problemId: string, rId: string) {
        return this.http.post(`${environment.apiBaseUrl}/problems/${problemId}/deletion-requests/approved`,
            {
                requestId: rId
            });
    }

    restoreProblem(problemId: string) {
        return this.http.patch(`${environment.apiBaseUrl}/problems/deleted/${problemId}`, null);
    }

    getTaskSlug() {
        return this.http.get(`${environment.apiBaseUrl}/problem-slug`, { responseType: 'text' })
    }

    createProblem(p: ProblemUpdateDto) {
        return this.http.post(`${environment.apiBaseUrl}/problems`, p);
    }

    getUsers() {
        return this.http.get<UserDto[]>(`${environment.apiBaseUrl}/users`);
    }

    getUserRoles(userId: string) {
        return this.http.get<RoleDto[]>(`${environment.apiBaseUrl}/users/${userId}`);
    }

    getAvailableRoles() {
        return this.http.get<RoleDto[]>(`${environment.apiBaseUrl}/roles`);
    }

    updateUserRoles(userId: string, dto: UpdateRolesDto) {
        return this.http.patch(`${environment.apiBaseUrl}/user/${userId}/roles`, dto)
    }

    getUserMetadata(userId: string) {
        return this.http.get<UserMetadataDto>(`${environment.apiBaseUrl}/users/${userId}/metadata`);
    }

    softDeleteUser(userId: string) {
        return this.http.delete<UserMetadataDto | null>(`${environment.apiBaseUrl}/users/${userId}`);
    }

    cancelUserDeletion(userId: string) {
        return this.http.patch(`${environment.apiBaseUrl}/users/${userId}`, null);
    }
}