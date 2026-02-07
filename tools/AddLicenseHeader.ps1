$header = @"
/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
"@

Get-ChildItem -Path . -Filter *.cs -Recurse | ForEach-Object {
    $content = Get-Content -Raw -Path $_.FullName

    # Strip existing leading comment block if present
    if ($content.TrimStart().StartsWith("/*")) {
        $end = $content.IndexOf("*/")
        if ($end -ge 0) {
            $content = $content.Substring($end + 2).TrimStart("`r","`n")
        }
    }

    $newContent = "$header`r`n$content"
    Set-Content -Path $_.FullName -Value $newContent -NoNewline
}