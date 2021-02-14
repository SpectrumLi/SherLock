($foo = Remove-Item -Path temp -Force -Recurse) | out-null
($foo = mkdir -p temp) | out-null 

($foo = Remove-Item -Path sh-temp -Force -Recurse) | out-null
($foo = mkdir -p sh-temp) | out-null

cp .\TestApp\bin\Debug\Runtime.log temp\

$preprococess='..\..\log-analysis\log-preprocessing\Analyzer\bin\Debug\Analyzer.exe'

#cd temp
& $preprococess . | Out-Null

mkdir .\temp\1
mv .\temp\1.litelog .\temp\1
mv .\temp\3.litelog .\temp\1

$log_ana = '..\..\log-analysis\linear-solver\log_analyzer.py'
python $log_ana --batch temp