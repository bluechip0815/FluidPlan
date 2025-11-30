@echo off
setlocal

:: Force the command prompt to use UTF-8 to handle Umlauts/Special chars correctly
chcp 65001 >nul

:: --- CONFIGURATION ---
set "output_file=FullPromptData.txt"
set "header_file=prompt-header.txt"
set "search_dir=%~dp0"
set "file_pattern=*.cs"
:: ---------------------

:: Delete old file if it exists
if exist "%output_file%" del "%output_file%"

:: Copy header file as prefix
if exist "%header_file%" (
    type "%header_file%" > "%output_file%"
    echo. >> "%output_file%"
    echo. >> "%output_file%"
) else (
    echo WARNING: Header file "%header_file%" not found!
)

echo Scanning folder structure for %file_pattern%...

:: Use 'dir /s /b' which is more reliable than 'for /R' for finding full paths
(
    for /f "delims=" %%f in ('dir /s /b /a-d "%search_dir%%file_pattern%"') do (
        
        :: Print start tag
        echo ^<%%~nxf^>
        
        :: Print file content. The quotes "" handle spaces in paths safely.
        type "%%f"
        
        :: New lines and closing tag
        echo.
        echo ^</%%~nxf^>
        echo.
    )
) >> "%output_file%"

echo.
echo ========================================================
echo Done! All code has been collected into: %output_file%
echo ========================================================
pause