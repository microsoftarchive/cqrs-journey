## Microsoft patterns & practices
# CQRS Journey Reference Implementation

http://cqrsjourney.github.com

# Release Notes

The most up-to-date version of the release notes issues is available [online][releasenotes].

The release notes include information about:

* How to obtain the source code.
* How to configure the application.
* How to build the application.
* How to run the application.
* How to run the tests.
* Known issues.

# Contribution Guidelines Overview

If you would like to become involved in the development of the CQRS Journey sample application and guidance, you can contribute in many different ways:

* We strongly value user feedback and appreciate your questions, bug reports and feature requests. For more details how you can submit these, see the section "[Using the Reference Implementation, Reading the Documentation, and Providing Feedback](#sampleapp)" below. 
* You can also contribute changes to the code, which might include bug fixes and improvements as well as new features. See the section "[Contributing Changes to the Code](#changecode)" below.
* You can also revise or propose new content for the guide. See the section "[Contributing Changes to the Guide](#changeguide)" below.
* You can provide review comments to the draft guide. See the section "[Simple Document Review](#simplereview)" below.

<a name="sampleapp" />

## Using the Reference Implementation, Reading the Documentation, and Providing Feedback

Reviewing the CQRS Journey guidance and code, asking and answering questions, reporting bugs and making feature requests are critical activities of the project community: we value your feedback!

In order to become familiar with the functionality of the reference implementation you can obtain the source code from GitHub (see "[Obtaining the source code](#obtainsource)" below) and compile it locally. While you become familiar with the functionality, you can report bugs or request new features (see "[Report bugs and request features](#reportbugs)" below).   You can also download the current draft of the documentation from GitHub.

We would love to hear your thoughts, be it comments, suggestions, ideas or anything else. However, in the end we are creating Microsoft guidance. So we'll happily take your feedback but you need to understand that by providing us feedback in any form you are agreeing that (i) we may freely use, disclose, reproduce, modify, license, distribute and otherwise commercialize your feedback in  any Microsoft product, technology, service, specification or other documentation, (ii) others may use, disclose, reproduce, license, distribute and otherwise commercialize your feedback in connection with our products and services, and (iii) you will not be compensated for any of these things. We may incorporate ideas or make changes based on comments you make, or we may make changes to the product that are indirectly influenced by discussions that we have with you and other folks in the community. 

### Repositories overview

There are three project repositories:

1.  Source code repository:  [https://github.com/mspnp/cqrs-journey-code](https://github.com/mspnp/cqrs-journey-code)
2.	Document repository: [https://github.com/mspnp/cqrs-journey-doc](https://github.com/mspnp/cqrs-journey-doc)
3.	Wiki: [https://github.com/mspnp/cqrs-journey-wiki](https://github.com/mspnp/cqrs-journey-wiki)

We are also hosting our project web site on GitHub  - [http://cqrsjourney.github.com](http://cqrsjourney.github.com)

<a name="askanswer" />

### Asking and answering questions

You can ask questions via Disqus on the [http://cqrsjourney.github.com](http://cqrsjourney.github.com) web site or by posting them as Issues with the "question" prefix in GitHub to the project's **cqrs-journey-code** or **cqrs-journey-doc** repositories.

<a name="obtainsource" />

### Obtaining the source code

In order to obtain the source code you can either download it as a zip file from the GitHub or clone the repository by using Git. 

Follow these steps to download the source code in a zip file:

1.	Go to [https://github.com/mspnp/cqrs-journey-code](https://github.com/mspnp/cqrs-journey-code)
2.	Click on the ZIP button right below the project description.

To clone the source repository using Git, see instructions from the [Checkout the latest code](#checkoutlatest) section.

### Obtaining the draft content of the guide

The draft content of the guide is available in the **cqrs-journey-doc** repository on GitHub. You can read it online. You can also download or clone this repository in the same way that you obtain the source code.

<a name="reportbugs" />

### Report bugs and request features

Issues and feature requests are submitted through the project's Issues section on GitHub. Please use the following guidelines when you submit issues and feature requests:

* Make sure that you post the issue to the correct repository: either the **cqrs-journey-code** repository for code issues or **cqrs-journey-doc** repository for documentation issues.
* Make sure the issue is not already reported by searching through the list of issues. 
* Provide detailed description of the issue including the following information: 
	+ The feature the issue appears in.
	+ Under what circumstances the issue appears (repro steps).
	+ Relevant environmental/contextual information.
	+ The desired behavior.
	+ The actual behavior (what is breaking).
	+ The impact (such as loss or corruption of data, compromising security, disruption of service, etc.)
	+ Any code, screenshots, configuration files that will be helpful to reproduce the issue.

The core team regularly reviews issues and updates those with additional information. Sometimes the core team may have questions about particular issue that might need clarifications, so please be ready to provide additional information.  

<a name="changecode" />

## Contributing Changes to the Code

### How to become a contributor?
In order to become a contributor to the project you must sign the Contributor License Agreement (CLA). Signing the Contributor License Agreement (CLA) does not grant you rights to commit to the source code or doc repositories but it does mean that we will consider your contributions and you will get credit for them if we use them. 

You can download the Contributor License Agreement (CLA) by clicking the following link: [http://cqrsjourney.github.com/docs/Contribution%20License%20Agreement.pdf](http://cqrsjourney.github.com/docs/Contribution%20License%20Agreement.pdf). Please fill in, sign, scan and email it to [cla@microsoft.com](mailto:cla@microsoft.com) with the "CQRS Journey Project CLA" as the subject line.

You do not need to sign a separate agreement if you have already submitted one to contribute to the project's documentation or to other Microsoft OSS projects (such as Windows Azure SDK) and if your employer hasn't changed.

<a name="checkoutlatest" />

### Checkout the latest code

In order to obtain the source code you need to become familiar with Git (see [http://progit.org/book/](http://progit.org/book/)) and GitHub (see [http://help.github.com/](http://help.github.com/)) and you need to have Git installed on your local machine. 

You can obtain the source code from GitHub by following the following steps on your local machine:

1.	Go to https://github.com/mspnp/cqrs-journey-code
2.	Select the Fork button and choose your own GitHub account as target 
3.	Clone the repository on your local machine with the following Git command  
`git clone git@github.com:[USERNAME]/cqrs-journey-code`
4.	Add a remote to your local repository using the following Git commands  
`cd cqrs-journey-code`  
`git remote add upstream git@github.com:mspnp/cqrs-journey-code`
5.	Update your local repository with the changes from the remote repository by using the following Git commands  
`git fetch upstream/dev`  
`git merge upstream/dev`

### Create bug fixes and features

We are using the [Fork+Pull Model](http://help.github.com/send-pull-requests/) of collaborative development.

You make modifications of the code and commit them in your local Git repository. Once you are done with your implementation follow the steps below:

1.	Change the working branch to dev with the following command  
`git checkout dev `
2.	Submit the changes to your own fork in GitHub by using the following command  
`git push`
3.	In GitHub create new pull request by clicking on the Pull Request button  
4.	In the pull request select your fork as the head and mspnp/cqrs-journey-code as base for the request 
5.	Write detailed message describing the changes in the pull request 
6.	Submit the pull request for consideration by the core team 

**Note:** It's a good idea to create a branch before submitting a pull request, just in case there are improvement suggestions to the submitted contribution that need to be incorporated before it's accepted. That way, the pull request points to the entire branch and changes to it can be incorporated using the same pull request sent initially (which will hold discussions and comments about the improvements, etc.).

**Note:** All changes and pull request should be done against the `dev` branch. Changes will be integrated in the `master` branch by the core team.

Please keep in mind that not all requests will be approved. Requests are reviewed by the core team on a regular basis and will be updated with the status at each review. If your request is rejected you will receive information about the reasons why it was rejected.

### Contribution guidelines

Before you start working on bug fixes and features it is good idea to discuss those broadly with the community. You can use the forums as described in [Asking and answering questions](#askanswer) for this purpose.

Before submitting your changes make sure you followed the guidelines below:

* For every new code file, include the file header with license information that is included in all other files (see [https://github.com/mspnp/cqrs-journey-code/blob/dev/source/CQRS-journey.licenseheader](https://github.com/mspnp/cqrs-journey-code/blob/dev/source/CQRS-journey.licenseheader), for example).
* You have properly documented any new functionality using the documentation standards for the language (this includes classes, methods and functions, properties etc.). For any change you make proper inline documentation is included.
* For any new functionality or updates, you have written complete unit tests.
* You have run all unit tests and they pass.
* You have run the Stylecop and most default rules are satisfied.
* Code should fit into the overall structure of the project and style of the existing codebase.

In order to speed up the process of accepting your contributions, you should try to make your check-ins as small as possible, avoid any unnecessary deltas and the need to rebase.

<a name="changeguide" />

## Contributing Changes to the Guide

### How to become a contributor?

In order to become a contributor to the project you must sign the Contributor License Agreement (CLA). Signing the Contributor License Agreement (CLA) does not grant you rights to commit to the source code or doc repositories but it does mean that we will consider your contributions and you will get credit for them if we use them. 

You can download the Contributor License Agreement (CLA) by clicking at the following link: [http://cqrsjourney.github.com/docs/Contribution%20License%20Agreement.pdf](http://cqrsjourney.github.com/docs/Contribution%20License%20Agreement.pdf). Please fill in, sign, scan and email it to [cla@microsoft.com](mailto:cla@microsoft.com).

You do not need to sign a separate agreement if you have already submitted one to contribute to the project's source code or to other Microsoft OSS projects (such as Windows Azure SDK) and if your employer hasn't changed.

### Checkout the latest version of the guide

In order to obtain the latest docs you need to become familiar with Git (see [http://progit.org/book/](http://progit.org/book/)) and GitHub (see [http://help.github.com/](http://help.github.com/)) and you need to have Git installed on your local machine. 

You can obtain the docs from GitHub by following the following steps on your local machine:

1.	Go to https://github.com/mspnp/cqrs-journey-doc
2.	Select the Fork button   and choose your own GitHub account as target 
3.	Clone the repository on your local machine with the following Git command  
`git clone git@github.com:[USERNAME]/cqrs-journey-doc`
4.	Add remote to your local repository using the following Git commands  
`cd cqrs-journey-doc`  
`git remote add upstream git@github.com:mspnp/cqrs-journey-doc`
5.	Update your local repository with the changes from the remote repository by using the following Git commands  
`git fetch upstream/dev`  
`git merge upstream/dev`

### Create document updates

We are using the [Fork+Pull](http://help.github.com/send-pull-requests/) Model of collaborative development.

You make modifications of the docs in your local Git repository. Once you are done with your changes follow the steps below:

1.	Change the working branch to `dev` with the following command  
`git checkout dev `
2.	Submit the changes to your own fork in GitHub by using the following command  
`git push`
3.	In GitHub create new pull request by clicking on the Pull Request button  
4.	In the pull request select your fork as source and mspnp/cqrs-journey-doc as destination for the request 
5.	Write detailed message describing the changes in the pull request 
6.	Submit the pull request for consideration by the core team 

**Note:** All changes and pull request should be done in the `dev` branch. Changes will be integrated in the `master` branch by the core team.

Please keep in mind that not all requests will be approved. Requests are reviewed by the core team on a regular basis and will be updated with the status at each review. If your request is rejected you will receive information about the reasons why it was rejected.

### Contribution guidelines

Before you start working on new document sections or make major revisions to the existing ones it is good idea to discuss those broadly with the community. You can use the forums as described in [Asking and answering](#askanswer) questions for this purpose.

Before submitting your revisions to the doc repository, make use you followed the guidelines below:

* Use [GitHub Flavored Markdown](http://github.github.com/github-flavored-markdown/) for all written documents. 
* Ensure that your text is fully spell-checked.
* Avoid using any embedded HTML code in the documents.
* Provide any diagrams in both SVG and PNG formats.
* Link to the PNG image in your markdown code.
* Add all links (including links to images) using reference style links (see [Markdown:syntax](http://daringfireball.net/projects/markdown/syntax) for more details).
* Try to adher to the style/tone of the existing documents in the repository.

In order to speed up the process of accepting your contributions, you should try to make your check-ins as small as possible, avoid any unnecessary deltas and the need to rebase.

<a name="simplereview" />

## Simple document review

An easy way to review the docs and comment on them (without using Git, GitHub, or Markdown) is via the [simple document review site](http://pundit.cloudapp.net/#Journey_00_Preface.doc), which allows you to see the docs with inline comments of others and submit your own commentary. Please note that this site only shows the latest version of the docs. As the new versions of the docs are produced and the comments are acted upon, the comments are removed from the pages but remain visible via the history of check-ins in the main document repository ([https://github.com/mspnp/cqrs-journey-doc](https://github.com/mspnp/cqrs-journey-doc)). 

##Inquiries

For any further inquiries, contact the project leader, Grigori Melnik, at _grigori dot melnik at microsoft dot com_

[releasenotes]: https://github.com/mspnp/cqrs-journey/blob/master/docs/Appendix1_Running.markdown
