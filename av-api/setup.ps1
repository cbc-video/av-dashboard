function Install-NeededFor {
param(
   [string] $packageName = ''
  ,[bool] $defaultAnswer = $true
)
  if ($packageName -eq '') {return $false}

  $yes = '6'
  $no = '7'
  $msgBoxTimeout='-1'
  $defaultAnswerDisplay = 'Yes'
  $buttonType = 0x4;
  if (!$defaultAnswer) { $defaultAnswerDisplay = 'No'; $buttonType= 0x104;}

  $answer = $msgBoxTimeout
  try {
    $timeout = 10
    $question = "Do you need to install $($packageName)? Defaults to `'$defaultAnswerDisplay`' after $timeout seconds"
    $msgBox = New-Object -ComObject WScript.Shell
    $answer = $msgBox.Popup($question, $timeout, "Install $packageName", $buttonType)
  }
  catch {
  }

  if ($answer -eq $yes -or ($answer -eq $msgBoxTimeout -and $defaultAnswer -eq $true)) {
    write-host "Installing $packageName"
    return $true
  }

  write-host "Not installing $packageName"
  return $false
}


# Install Chocolatey
if (Install-NeededFor 'chocolatey') {
  iex ((new-object net.webclient).DownloadString('http://chocolatey.org/install.ps1'))
}

if (Install-NeededFor 'nginx') {
  cinst nginx
  cinst ruby -version 1.9.3.54500
}

if (Install-NeededFor 'nssm') {
    cinst nssm
    #todo add call for creating service nssm create nginx pathToExe
}

