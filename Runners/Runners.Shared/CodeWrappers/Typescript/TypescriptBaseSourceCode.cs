namespace Runners.Shared.CodeWrappers.Typescript
{
    public static class TypescriptBaseSourceCode
    {
        public const string Source = """
            // TypeScript runner bootstrap (auto-generated)
            // NOTE: This file is TypeScript but uses plain JS-compatible runtime constructs
            // for maximum compatibility with tsc/ts-node execution.

            'use strict';

            /* banned modules list will be injected here */
            const bannedModules: string[] = {{BANNED_MODULES}};

            (function() {
              try {
                const Module = require('module');
                const originalRequire = Module.prototype.require;
                Module.prototype.require = function(moduleName: string) {
                  try {
                    if (bannedModules && bannedModules.includes(moduleName)) {
                      throw new Error('[SECURITY] Importing module \"' + moduleName + '\" is not allowed.');
                    }
                  } catch (e) {
                    throw e;
                  }
                  return originalRequire.apply(this, arguments);
                };
              } catch (e) {
                // If module system not available, do nothing.
              }
            })();

            function __formatErrorForOutput(err: any, userPrefix?: string): string {
              userPrefix = userPrefix || '';
              if (!err) return userPrefix + 'Unknown error';
              const emsg = (err && (err as any).message) ? (err as any).message : String(err);
              let msg = emsg;
              try {
                const stack = (err && (err as any).stack) ? String((err as any).stack) : null;
                if (stack) {
                  // take first stack line after message if present
                  const lines = stack.split('\n').map((l: string) => l.trim()).filter((l: string) => l.length > 0);
                  if (lines.length > 1) {
                    msg += ' at ' + lines[1];
                  }
                }
              } catch(e) {}
              // remove newlines to keep a single-line reason and normalize whitespace
              msg = msg.replace(/\r?\n/g, ' ').replace(/\s+/g, ' ').trim();
              return userPrefix + msg;
            }
            
            // write failure marker to both stderr and stdout so parsers see it
            function logTestFail(name: string, reason: string, detailed?: any) {
              // sanitize reason for single-line marker
              const oneLine = String(reason).replace(/\r?\n/g, ' ').replace(/\s+/g, ' ').trim();
              try { console.log(`FailedTest:${name}:${oneLine}`); } catch(e) {}
              // write a more verbose version to stderr (stack etc.) for debugging
              try {
                if (detailed) {
                  // if caller passed original error object, print stack if available
                  if ((detailed as any).stack) {
                    try { process.stderr.write(`[FAILED-DETAIL] ${name}: ${String((detailed as any).stack)}\n`); } catch(e) {}
                  } else {
                    try { process.stderr.write(`[FAILED-DETAIL] ${name}: ${String(detailed)}\n`); } catch(e) {}
                  }
                } else {
                  // fallback: print the one-line reason to stderr too (less duplication)
                  try { process.stderr.write(`[FAILED-REASON] ${name}: ${oneLine}\n`); } catch(e) {}
                }
              } catch(e) {}
            }

            const RunnerHelpers = {
              deepEqual(a: any, b: any): boolean {
                try { return JSON.stringify(a) === JSON.stringify(b); } catch (e) { return a === b; }
              },

              asArray(x: any): any[] | null {
                if (x == null) return null;
                if (Array.isArray(x)) return x;
                if (x instanceof Set) return Array.from(x);
                if (x instanceof Map) return Array.from(x.entries());
                if (typeof x === 'string') return x.split('');
                if (typeof x === 'object') return Object.values(x);
                return null;
              },

              seqEqual(a: any, e: any) {
                const aa = this.asArray(a);
                const ee = this.asArray(e);
                if (aa == null || ee == null) return { ok: false, reason: 'seq_eq requires collections on both sides' };
                if (aa.length !== ee.length) return { ok: false, reason: `length mismatch: actual=${aa.length}, expected=${ee.length}` };
                for (let i = 0; i < aa.length; i++) {
                  if (!this.deepEqual(aa[i], ee[i])) return { ok: false, reason: `element mismatch at index ${i}` };
                }
                return { ok: true };
              },

              seqEqualSorted(a: any, e: any) {
                const aa = this.asArray(a);
                const ee = this.asArray(e);
                if (aa == null || ee == null) return { ok: false, reason: 'seq_eq_sorted requires collections on both sides' };
                if (aa.length !== ee.length) return { ok: false, reason: `length mismatch: actual=${aa.length}, expected=${ee.length}` };
                const countMap = (arr: any[]) => {
                  const m = new Map<string, number>();
                  for (const it of arr) {
                    let key: string;
                    try { key = JSON.stringify(it); } catch { key = String(it); }
                    m.set(key, (m.get(key) || 0) + 1);
                  }
                  return m;
                };
                const ma = countMap(aa);
                const me = countMap(ee);
                if (ma.size !== me.size) return { ok: false, reason: 'different element sets' };
                for (const [k, v] of ma.entries()) {
                  if (me.get(k) !== v) return { ok: false, reason: `element counts differ for ${k}` };
                }
                return { ok: true };
              },

              contains(actual: any, expected: any) {
                const a = this.asArray(actual);
                if (a != null) {
                  for (const el of a) if (this.deepEqual(el, expected)) return { ok: true };
                  return { ok: false, reason: 'actual does not contain expected element' };
                }
                const e = this.asArray(expected);
                if (e != null) {
                  for (const el of e) if (this.deepEqual(el, actual)) return { ok: true };
                  return { ok: false, reason: 'expected does not contain actual element' };
                }
                return { ok: false, reason: 'contains requires one side to be a collection' };
              },

              numericCompare(actual: any, expected: any, cmpFn: (a: number, b: number) => boolean, comparatorName: string) {
                const an = Number(actual);
                const bn = Number(expected);
                if (Number.isNaN(an) || Number.isNaN(bn)) return { ok: false, reason: comparatorName + ' requires numeric operands' };
                try {
                  if (cmpFn(an, bn)) return { ok: true };
                  return { ok: false, reason: comparatorName + ' comparison failed (' + an + ' vs ' + bn + ')' };
                } catch (e) {
                  const em = (e && (e as any).message) ? (e as any).message : String(e);
                  return { ok: false, reason: comparatorName + ' error: ' + em };
                }
              },

              assertCompare(actual: any, expected: any, comparator?: string, testName?: string) {
                comparator = (comparator || 'eq').toLowerCase();
                testName = testName || '';
                switch (comparator) {
                  case 'eq': return this.deepEqual(actual, expected) ? { ok: true } : { ok: false, reason: (testName ? testName + ': ' : '') + 'values not equal' };
                  case 'neq': return this.deepEqual(actual, expected) ? { ok: false, reason: (testName ? testName + ': ' : '') + 'values equal but expected not equal' } : { ok: true };
                  case 'seq_eq': return this.seqEqual(actual, expected);
                  case 'seq_eq_sorted': return this.seqEqualSorted(actual, expected);
                  case 'contains': return this.contains(actual, expected);
                  case 'lt': return this.numericCompare(actual, expected, (a,b) => a < b, 'lt');
                  case 'gt': return this.numericCompare(actual, expected, (a,b) => a > b, 'gt');
                  case 'le': return this.numericCompare(actual, expected, (a,b) => a <= b, 'le');
                  case 'ge': return this.numericCompare(actual, expected, (a,b) => a >= b, 'ge');
                  default: return { ok: false, reason: `Comparator \"${comparator}\" not supported` };
                }
              }
            };

            const __TestMonitor = {
              _passed: 0,
              inc() { this._passed = this._passed + 1; },
              get() { return this._passed; }
            };

            var __hadFailures = false;
            """;
    }
}
