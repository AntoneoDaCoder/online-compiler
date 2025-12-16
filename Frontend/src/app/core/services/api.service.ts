import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environment';
import { LanguageDto, ProblemDto, EditorProblemVersionDto, ShortSubmissionDto, SubmissionDto } from '../models/dtos';


@Injectable({ providedIn: 'root' })
export class ApiService {
    constructor(private http: HttpClient) { }


    getLanguages() {
        return this.http.get<LanguageDto[]>(`${environment.apiBaseUrl}/languages`);
    }


    getProblems() {
        return this.http.get<ProblemDto[]>(`${environment.apiBaseUrl}/problems`);
    }


    getEditorVersion(versionId: string) {
        return this.http.get<EditorProblemVersionDto>(`${environment.apiBaseUrl}/problems/versions/${versionId}`);
    }


    saveProblemVersion(problemId: string, dto: any) {
        return this.http.post(`${environment.apiBaseUrl}/problems/${problemId}/versions`, dto);
    }


    getVersions() {
        return this.http.get<EditorProblemVersionDto[]>(`${environment.apiBaseUrl}/problems/versions`);
    }


    publishVersion(versionId: string) {
        return this.http.post(`${environment.apiBaseUrl}/problems/versions/${versionId}/publish`, {});
    }


    deleteVersion(versionId: string) {
        return this.http.delete(`${environment.apiBaseUrl}/problems/versions/${versionId}`);
    }


    getSubmissions() {
        return this.http.get<ShortSubmissionDto[]>(`${environment.apiBaseUrl}/submissions`);
    }


    getSubmission(submissionId: string) {
        return this.http.get<SubmissionDto>(`${environment.apiBaseUrl}/submissions/${submissionId}`);
    }


    deleteSubmission(submissionId: string) {
        return this.http.delete(`${environment.apiBaseUrl}/submissions/${submissionId}`);
    }


    submitCode(dto: any) {
        return this.http.post(`${environment.apiBaseUrl}/submissions`, dto);
    }
}