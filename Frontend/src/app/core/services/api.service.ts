import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environment';
import { LanguageDto, ProblemDto, EditorProblemVersionDto, ShortSubmissionDto, SubmissionDto, UserProblemVersionDto } from '../models/dtos';


@Injectable({ providedIn: 'root' })
export class ApiService {
    constructor(private http: HttpClient) { }


    getLanguages() {
        return this.http.get<LanguageDto[]>(`${environment.apiBaseUrl}/languages`);
    }


    getProblems() {
        return this.http.get<ProblemDto[]>(`${environment.apiBaseUrl}/problems`);
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
}