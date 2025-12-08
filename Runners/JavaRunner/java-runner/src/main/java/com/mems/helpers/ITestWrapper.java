// ITestWrapper.java
package com.mems.helpers;

import com.mems.manifest.ManifestDto;

public interface ITestWrapper {
    /**
     * Generate full Java source (a single top-level class, ready to compile)
     */
    String generateSource(ManifestDto manifest, String languageCode, String userCode, String entrypointContainerClass, long defaultTimeoutMs)
            throws Exception;
}
