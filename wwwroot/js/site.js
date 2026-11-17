// JobGuard AI - Client-Side Interactions & Sample Pre-fills

document.addEventListener("DOMContentLoaded", function () {
    // Quick Demo Sample Data Pre-fills for Analyzer Page
    const btnLoadScamSample = document.getElementById("btnLoadScamSample");
    const btnLoadLegitSample = document.getElementById("btnLoadLegitSample");

    if (btnLoadScamSample) {
        btnLoadScamSample.addEventListener("click", function () {
            document.getElementById("JobTitle").value = "Immediate Work from Home Data Entry Assistant - Earn $45/hr No Experience";
            document.getElementById("CompanyName").value = "Apex Global Advisory Firm";
            document.getElementById("CompanyWebsite").value = "";
            document.getElementById("JobLocation").value = "Remote / Nationwide / Work from Home";
            document.getElementById("EmploymentType").value = "Part-time";
            document.getElementById("JobDescription").value = 
                "URGENT HIRING: We are seeking immediate home data entry clerks to earn $45/hr with flexible hours (10-20 hrs/week). Immediate start with immediate sign-on bonuses. Tasks include transferring digital invoice records, processing client wire vouchers, and receiving packages from your home.\n\nINTERVIEW PROCESS: Quick screening via Telegram app (Contact: @apex_recruiter_fast). Company will mail an initial certified check to purchase dedicated home office hardware package through our vendor portal. You must deposit check and wire payment for hardware before starting. Receive weekly payments via Western Union, wire transfer, or check.";
            document.getElementById("JobRequirements").value = "No experience required. Must have smartphone, internet connection, bank account, and be available for immediate start. High school diploma.";
            document.getElementById("CompanyProfile").value = "We are an international global marketing firm seeking immediate representatives.";

            const logoCheck = document.getElementById("HasCompanyLogo");
            if (logoCheck) logoCheck.checked = false;

            const questionsCheck = document.getElementById("HasQuestions");
            if (questionsCheck) questionsCheck.checked = false;

            const expSelect = document.getElementById("RequiredExperience");
            if (expSelect) expSelect.value = "Entry level";

            const eduSelect = document.getElementById("RequiredEducation");
            if (eduSelect) eduSelect.value = "High School or equivalent";

            flashFormHighlight();
        });
    }

    if (btnLoadLegitSample) {
        btnLoadLegitSample.addEventListener("click", function () {
            document.getElementById("JobTitle").value = "Senior C# .NET Core Software Engineer";
            document.getElementById("CompanyName").value = "Datastream Enterprise Technologies Inc.";
            document.getElementById("CompanyWebsite").value = "https://www.datastreamtech.com";
            document.getElementById("JobLocation").value = "Chicago, IL (Hybrid)";
            document.getElementById("EmploymentType").value = "Full-time";
            document.getElementById("JobDescription").value = 
                "Datastream Enterprise Technologies is seeking a Senior C# .NET Core Software Engineer to join our cloud platform engineering team. Responsibilities include building enterprise RESTful APIs using ASP.NET Core, EF Core, and SQL Server, participating in agile sprint ceremonies, and maintaining CI/CD automated test suites. We offer competitive base compensation, comprehensive health benefits, and 401(k) matching.";
            document.getElementById("JobRequirements").value = "5+ years of software development experience with C# and .NET Core. Proficiency in relational database design, async programming, and API security. Bachelor's degree in Computer Science or equivalent.";
            document.getElementById("CompanyProfile").value = "Established enterprise fintech software provider with over 500 global employees and certified ISO security compliance.";

            const logoCheck = document.getElementById("HasCompanyLogo");
            if (logoCheck) logoCheck.checked = true;

            const questionsCheck = document.getElementById("HasQuestions");
            if (questionsCheck) questionsCheck.checked = true;

            const expSelect = document.getElementById("RequiredExperience");
            if (expSelect) expSelect.value = "Mid-Senior level";

            const eduSelect = document.getElementById("RequiredEducation");
            if (eduSelect) eduSelect.value = "Bachelor's Degree";

            flashFormHighlight();
        });
    }

    function flashFormHighlight() {
        const form = document.getElementById("jobAnalysisForm");
        if (form) {
            form.style.transition = "box-shadow 0.3s ease";
            form.style.boxShadow = "0 0 25px rgba(221, 187, 66, 0.4)";
            setTimeout(() => {
                form.style.boxShadow = "none";
            }, 600);
        }
    }
});
