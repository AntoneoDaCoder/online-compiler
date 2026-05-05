namespace Runners.Shared.CodeWrappers.NodeJs
{
    public static class NodeBaseSourceCode
    {
        public const string Source =
   """
   'use strict';

   /* banned modules list will be injected here */
   const bannedModules = {{BANNED_MODULES}};

   (function() {
     try {
       const Module = require('module');
       const originalRequire = Module.prototype.require;
       Module.prototype.require = function(moduleName) {
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
     }
   })();

   function __formatErrorForOutput(err, userPrefix) {
     userPrefix = userPrefix || '';
     if (!err) return userPrefix + 'Unknown error';
     var msg = (err && err.message) ? err.message : String(err);
     if (err && err.stack) {
       try {
         var lines = String(err.stack).split('\n').map(function(l){ return l.trim(); }).filter(function(l){ return l.length > 0; });
         if (lines.length > 1) {
           msg += ' at ' + lines[1];
         }
       } catch (e) {}
     }
     return userPrefix + msg;
   }

   const RunnerHelpers = {
     deepEqual: function(a, b) {
       try {
         return JSON.stringify(a) === JSON.stringify(b);
       } catch (e) {
         return a === b;
       }
     },

     asArray: function(x) {
       if (x == null) return null;
       if (Array.isArray(x)) return x;
       if (x instanceof Set) return Array.from(x);
       if (x instanceof Map) return Array.from(x.entries());
       if (typeof x === 'string') return x.split('');
       if (typeof x === 'object') return Object.values(x);
       return null;
     },

     seqEqual: function(a, e) {
       var aa = this.asArray(a);
       var ee = this.asArray(e);
       if (aa == null || ee == null) return { ok: false, reason: 'seq_eq requires collections on both sides' };
       if (aa.length !== ee.length) return { ok: false, reason: 'length mismatch: actual=' + aa.length + ', expected=' + ee.length };
       for (var i = 0; i < aa.length; i++) {
         if (!this.deepEqual(aa[i], ee[i])) return { ok: false, reason: 'element mismatch at index ' + i };
       }
       return { ok: true };
     },

     seqEqualSorted: function(a, e) {
       var aa = this.asArray(a);
       var ee = this.asArray(e);
       if (aa == null || ee == null) return { ok: false, reason: 'seq_eq_sorted requires collections on both sides' };
       if (aa.length !== ee.length) return { ok: false, reason: 'length mismatch: actual=' + aa.length + ', expected=' + ee.length };
       var countMap = function(arr) {
         var m = new Map();
         for (var i = 0; i < arr.length; i++) {
           var key;
           try { key = JSON.stringify(arr[i]); } catch (e) { key = String(arr[i]); }
           m.set(key, (m.get(key) || 0) + 1);
         }
         return m;
       };
       var ma = countMap(aa);
       var me = countMap(ee);
       if (ma.size !== me.size) return { ok: false, reason: 'different element sets' };
       var it = ma.entries();
       for (var entry = it.next(); !entry.done; entry = it.next()) {
         var k = entry.value[0], v = entry.value[1];
         if (me.get(k) !== v) return { ok: false, reason: 'element counts differ for ' + k };
       }
       return { ok: true };
     },

     contains: function(actual, expected) {
       var a = this.asArray(actual);
       if (a != null) {
         for (var i = 0; i < a.length; i++) if (this.deepEqual(a[i], expected)) return { ok: true };
         return { ok: false, reason: 'actual does not contain expected element' };
       }
       var e = this.asArray(expected);
       if (e != null) {
         for (var j = 0; j < e.length; j++) if (this.deepEqual(e[j], actual)) return { ok: true };
         return { ok: false, reason: 'expected does not contain actual element' };
       }
       return { ok: false, reason: 'contains requires one side to be a collection' };
     },

     numericCompare: function(actual, expected, cmpFn, comparatorName) {
       var an = Number(actual);
       var bn = Number(expected);
       if (Number.isNaN(an) || Number.isNaN(bn)) return { ok: false, reason: comparatorName + ' requires numeric operands' };
       try {
         if (cmpFn(an, bn)) return { ok: true };
         return { ok: false, reason: comparatorName + ' comparison failed (' + an + ' vs ' + bn + ')' };
       } catch (e) {
         return { ok: false, reason: comparatorName + ' error: ' + (e && e.message ? e.message : String(e)) };
       }
     },

     assertCompare: function(actual, expected, comparator, testName) {
       comparator = (comparator || 'eq').toLowerCase();
       testName = testName || '';
       switch (comparator) {
         case 'eq':
           return this.deepEqual(actual, expected) ? { ok: true } : { ok: false, reason: (testName ? testName + ': ' : '') + 'values not equal' };
         case 'neq':
           return this.deepEqual(actual, expected) ? { ok: false, reason: (testName ? testName + ': ' : '') + 'values equal but expected not equal' } : { ok: true };
         case 'seq_eq':
           return this.seqEqual(actual, expected);
         case 'seq_eq_sorted':
           return this.seqEqualSorted(actual, expected);
         case 'contains':
           return this.contains(actual, expected);
         case 'lt':
           return this.numericCompare(actual, expected, function(a,b){ return a < b; }, 'lt');
         case 'gt':
           return this.numericCompare(actual, expected, function(a,b){ return a > b; }, 'gt');
         case 'le':
           return this.numericCompare(actual, expected, function(a,b){ return a <= b; }, 'le');
         case 'ge':
           return this.numericCompare(actual, expected, function(a,b){ return a >= b; }, 'ge');
         default:
           return { ok: false, reason: 'Comparator \"' + comparator + '\" not supported' };
       }
     }
   };

   const __TestMonitor = {
     _passed: 0,
     _failed: [],
     inc: function() { this._passed = this._passed + 1; },
     addFailure: function(name, reason) {
       this._failed.push({ name: name || 'unknown', reason: reason || 'Unknown error' });
     },
     getPassed: function() { return this._passed; },
     getFailed: function() { return this._failed.slice(); }
   };

   var __hadFailures = false;
   """;
    }

}
