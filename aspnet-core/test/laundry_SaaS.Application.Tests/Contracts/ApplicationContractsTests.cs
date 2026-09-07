using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using laundry_SaaS.Bags;
using laundry_SaaS.Catalog;
using laundry_SaaS.Complaints;
using laundry_SaaS.Customers;
using laundry_SaaS.Drivers;
using laundry_SaaS.HostManagement;
using laundry_SaaS.Inspections;
using laundry_SaaS.Laundries;
using laundry_SaaS.Notifications;
using laundry_SaaS.Orders;
using laundry_SaaS.PickupDelivery;
using Shouldly;
using Volo.Abp.Application.Services;
using Xunit;

namespace laundry_SaaS.Contracts;

public class ApplicationContractsTests
{
    [Fact]
    public void All_Fifteen_Application_Service_Interfaces_Must_Exist_And_Inherit_IApplicationService()
    {
        var interfaceTypes = new[]
        {
            typeof(ILaundryHostAppService),
            typeof(ILaundryProfileAppService),
            typeof(ILaundryTimeSlotAppService),
            typeof(ILaundryStaffAppService),
            typeof(ILaundryCatalogAppService),
            typeof(ICustomerCatalogAppService),
            typeof(ICustomerProfileAppService),
            typeof(ICustomerOrderAppService),
            typeof(ILaundryOrderAppService),
            typeof(IDriverManagementAppService),
            typeof(IDriverTaskAppService),
            typeof(IBagAppService),
            typeof(IInspectionAppService),
            typeof(IAppNotificationAppService),
            typeof(IComplaintAppService)
        };

        interfaceTypes.Length.ShouldBe(15);

        foreach (var type in interfaceTypes)
        {
            type.IsInterface.ShouldBeTrue($"{type.Name} should be an interface");
            typeof(IApplicationService).IsAssignableFrom(type).ShouldBeTrue($"{type.Name} should inherit from IApplicationService");
        }
    }

    [Fact]
    public void Point1_PickupScheduleDto_Must_Be_Official_Dto_In_Inventory()
    {
        var type = typeof(PickupScheduleDto);
        type.ShouldNotBeNull();
        type.IsClass.ShouldBeTrue();
        type.Namespace.ShouldBe("laundry_SaaS.Orders");

        var properties = type.GetProperties().Select(p => p.Name).ToList();
        properties.ShouldContain("ScheduledDate");
        properties.ShouldContain("OriginalSlotId");
        properties.ShouldContain("StartTime");
        properties.ShouldContain("EndTime");
    }

    [Fact]
    public void Point2_And_3_CustomerOrderDetailDto_And_LaundryOrderDetailDto_Must_Not_Contain_PaymentMethod()
    {
        var customerOrderProps = typeof(CustomerOrderDetailDto).GetProperties().Select(p => p.Name).ToList();
        customerOrderProps.ShouldNotContain("PaymentMethod");

        var laundryOrderProps = typeof(LaundryOrderDetailDto).GetProperties().Select(p => p.Name).ToList();
        laundryOrderProps.ShouldNotContain("PaymentMethod");
    }

    [Fact]
    public void Point4_ILaundryOrderAppService_Must_Not_Contain_ConfirmOrderAsync()
    {
        var methods = typeof(ILaundryOrderAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldNotContain("ConfirmOrderAsync");
        methods.ShouldNotContain("ConfirmOrder");
        methods.ShouldNotContain("ConfirmAsync");
    }

    [Fact]
    public void Point5_Driver_Interface_Must_Not_Contain_Assignment_Methods_And_DriverManagement_Must_Contain_Them()
    {
        var driverTaskMethods = typeof(IDriverTaskAppService).GetMethods().Select(m => m.Name).ToList();
        driverTaskMethods.ShouldNotContain("AssignPickupDriverAsync");
        driverTaskMethods.ShouldNotContain("AssignDeliveryDriverAsync");

        var driverMgmtMethods = typeof(IDriverManagementAppService).GetMethods().Select(m => m.Name).ToList();
        driverMgmtMethods.ShouldContain("AssignPickupDriverAsync");
        driverMgmtMethods.ShouldContain("AssignDeliveryDriverAsync");
    }

    [Fact]
    public void Point6_And_7_IDriverTaskAppService_Must_Reflect_PickupTask_State_Machine_Commands()
    {
        var methods = typeof(IDriverTaskAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldContain("AcceptPickupTaskAsync");
        methods.ShouldContain("StartPickupTripAsync");
        methods.ShouldContain("ArriveAtPickupAsync");
        methods.ShouldContain("ConfirmPickupAsync");
        methods.ShouldContain("CompletePickupAsync");
        methods.ShouldContain("FailPickupTaskAsync");
    }

    [Fact]
    public void Complete_Delivery_Workflow_Methods_In_IDriverTaskAppService()
    {
        var methods = typeof(IDriverTaskAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldContain("AcceptDeliveryTaskAsync");
        methods.ShouldContain("PickUpFromLaundryAsync");
        methods.ShouldContain("StartDeliveryTripAsync");
        methods.ShouldContain("ArriveAtDeliveryAsync");
        methods.ShouldContain("RequestDeliveryOtpAsync");
        methods.ShouldContain("VerifyDeliveryOtpAsync");
        methods.ShouldContain("ConfirmCashCollectionAsync");
        methods.ShouldContain("ConfirmDeliveryAsync");
        methods.ShouldContain("FailDeliveryTaskAsync");
    }

    [Fact]
    public void Point8_ICustomerProfileAppService_Must_Contain_SetDefaultAddressAsync()
    {
        var methods = typeof(ICustomerProfileAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldContain("SetDefaultAddressAsync");
    }

    [Fact]
    public void Point9_Inspection_Contract_Must_Contain_UpsertItemAsync_And_UpsertInspectionItemInput()
    {
        var methods = typeof(IInspectionAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldContain("UpsertItemAsync");

        var inputType = typeof(UpsertInspectionItemInput);
        inputType.ShouldNotBeNull();

        var props = inputType.GetProperties().Select(p => p.Name).ToList();
        props.ShouldContain("OrderItemId");
        props.ShouldContain("ActualLaundryItemTypeId");
        props.ShouldContain("ActualLaundryServiceId");
        props.ShouldContain("ActualQuantity");
        props.ShouldContain("Notes");

        // Client must not send expected values as truth
        props.ShouldNotContain("ExpectedQuantity");
        props.ShouldNotContain("ExpectedLaundryItemTypeId");
        props.ShouldNotContain("ExpectedLaundryServiceId");
    }

    [Fact]
    public void Point10_And_11_CreateOrderAdjustmentInput_Must_Not_Contain_AdjustedItems_And_AdjustmentItemDiffInput_Must_Be_Removed()
    {
        var props = typeof(CreateOrderAdjustmentInput).GetProperties().Select(p => p.Name).ToList();
        props.ShouldNotContain("AdjustedItems");
        props.ShouldContain("InspectionId");
        props.ShouldContain("Reason");

        // Verify AdjustmentItemDiffInput does not exist in Application.Contracts assembly
        var contractsAssembly = typeof(CreateOrderAdjustmentInput).Assembly;
        var diffInputType = contractsAssembly.GetType("laundry_SaaS.Orders.AdjustmentItemDiffInput");
        diffInputType.ShouldBeNull("AdjustmentItemDiffInput must be completely removed from contracts.");
    }

    [Fact]
    public void Point12_IComplaintAppService_Must_Contain_ReviewAsync()
    {
        var methods = typeof(IComplaintAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldContain("ReviewAsync");
        methods.ShouldContain("ResolveAsync");
        methods.ShouldContain("CloseAsync");
    }

    [Fact]
    public void Point13_ILaundryHostAppService_Must_Contain_ActivateLaundryTenantAsync_And_SuspendLaundryTenantAsync()
    {
        var methods = typeof(ILaundryHostAppService).GetMethods().Select(m => m.Name).ToList();
        methods.ShouldContain("ActivateLaundryTenantAsync");
        methods.ShouldContain("SuspendLaundryTenantAsync");
    }

    [Fact]
    public void Point14_No_Input_DTO_Must_Contain_BagEventType()
    {
        var contractsAssembly = typeof(CreateOrderInput).Assembly;
        var inputTypes = contractsAssembly.GetExportedTypes()
            .Where(t => t.Name.EndsWith("Input"))
            .ToList();

        inputTypes.Count.ShouldBeGreaterThan(0);

        foreach (var inputType in inputTypes)
        {
            var props = inputType.GetProperties().Select(p => p.Name).ToList();
            props.ShouldNotContain("BagEventType", $"Type {inputType.Name} should not contain BagEventType");
            if (inputType == typeof(ScanBagQrInput))
            {
                props.ShouldNotContain("EventType");
            }
        }
    }

    [Fact]
    public void Point15_No_Raw_Payment_Types_In_Contracts()
    {
        var contractsAssembly = typeof(CreateOrderInput).Assembly;
        var allTypes = contractsAssembly.GetExportedTypes().Select(t => t.Name).ToList();

        allTypes.ShouldNotContain("Payment");
        allTypes.ShouldNotContain("PaymentDto");
        allTypes.ShouldNotContain("PaymentMethod");
        allTypes.ShouldNotContain("PaymentStatus");
        allTypes.ShouldNotContain("CreatePaymentInput");
    }

    [Fact]
    public void Catalog_Lifecycle_Methods_Must_Be_Complete()
    {
        var methods = typeof(ILaundryCatalogAppService).GetMethods().Select(m => m.Name).ToList();

        // Item Types
        methods.ShouldContain("GetItemTypesAsync");
        methods.ShouldContain("GetItemTypeAsync");
        methods.ShouldContain("CreateItemTypeAsync");
        methods.ShouldContain("UpdateItemTypeAsync");
        methods.ShouldContain("SetItemTypeActiveAsync");
        methods.ShouldContain("DeleteItemTypeAsync");

        // Services
        methods.ShouldContain("GetServicesAsync");
        methods.ShouldContain("GetServiceAsync");
        methods.ShouldContain("CreateServiceAsync");
        methods.ShouldContain("UpdateServiceAsync");
        methods.ShouldContain("SetServiceActiveAsync");
        methods.ShouldContain("DeleteServiceAsync");

        // Prices
        methods.ShouldContain("GetServicePricesAsync");
        methods.ShouldContain("GetServicePriceAsync");
        methods.ShouldContain("SetServicePriceAsync");
        methods.ShouldContain("SetServicePriceActiveAsync");
        methods.ShouldContain("DeleteServicePriceAsync");
    }

    [Fact]
    public void TimeSlot_And_Staff_Lifecycle_Methods_Must_Use_SetActiveAsync()
    {
        var timeSlotMethods = typeof(ILaundryTimeSlotAppService).GetMethods().Select(m => m.Name).ToList();
        timeSlotMethods.ShouldContain("SetActiveAsync");
        timeSlotMethods.ShouldNotContain("DeleteAsync");

        var staffMethods = typeof(ILaundryStaffAppService).GetMethods().Select(m => m.Name).ToList();
        staffMethods.ShouldContain("SetActiveAsync");
        staffMethods.ShouldNotContain("DeleteAsync");
    }

    [Fact]
    public void CustomerOrderDetailDto_Must_Preserve_Privacy_And_Not_Contain_Internal_Ids()
    {
        var type = typeof(CustomerOrderDetailDto);
        var propertyNames = type.GetProperties().Select(p => p.Name).ToList();

        propertyNames.ShouldNotContain("TenantId");
        propertyNames.ShouldNotContain("CustomerId");
        propertyNames.ShouldNotContain("StaffIds");
    }

    [Fact]
    public void VerifyDeliveryOtpInput_Must_Validate_Exactly_Six_Digits()
    {
        var inputValid = new VerifyDeliveryOtpInput { OtpCode = "123456" };
        var resultsValid = new List<ValidationResult>();
        Validator.TryValidateObject(inputValid, new ValidationContext(inputValid), resultsValid, true).ShouldBeTrue();

        var inputInvalidAlpha = new VerifyDeliveryOtpInput { OtpCode = "12345A" };
        var resultsInvalidAlpha = new List<ValidationResult>();
        Validator.TryValidateObject(inputInvalidAlpha, new ValidationContext(inputInvalidAlpha), resultsInvalidAlpha, true).ShouldBeFalse();

        var inputInvalidLength = new VerifyDeliveryOtpInput { OtpCode = "12345" };
        var resultsInvalidLength = new List<ValidationResult>();
        Validator.TryValidateObject(inputInvalidLength, new ValidationContext(inputInvalidLength), resultsInvalidLength, true).ShouldBeFalse();

        var inputInvalidLong = new VerifyDeliveryOtpInput { OtpCode = "1234567" };
        var resultsInvalidLong = new List<ValidationResult>();
        Validator.TryValidateObject(inputInvalidLong, new ValidationContext(inputInvalidLong), resultsInvalidLong, true).ShouldBeFalse();
    }

    [Fact]
    public void ScanBagQrInput_Must_Not_Contain_BagEventType()
    {
        var type = typeof(ScanBagQrInput);
        var propertyNames = type.GetProperties().Select(p => p.Name).ToList();

        propertyNames.ShouldNotContain("BagEventType");
        propertyNames.ShouldNotContain("EventType");
        propertyNames.ShouldContain("QrCode");
    }

    [Fact]
    public void DeliveryOtpStatusDto_Must_Not_Contain_OtpHash()
    {
        var type = typeof(DeliveryOtpStatusDto);
        var propertyNames = type.GetProperties().Select(p => p.Name).ToList();

        propertyNames.ShouldNotContain("OtpHash");
        propertyNames.ShouldNotContain("PlainOtp");
        propertyNames.ShouldNotContain("Salt");
    }

    [Fact]
    public void Exact_Fifteen_Approved_Application_Service_Interfaces_Must_Exist_With_No_Phantom_Interfaces()
    {
        var contractsAssembly = typeof(ILaundryHostAppService).Assembly;
        var appServiceInterfaces = contractsAssembly.GetExportedTypes()
            .Where(t => t.IsInterface && typeof(IApplicationService).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();

        var expectedInterfaceNames = new[]
        {
            "IAppNotificationAppService",
            "IBagAppService",
            "IComplaintAppService",
            "ICustomerCatalogAppService",
            "ICustomerOrderAppService",
            "ICustomerProfileAppService",
            "IDriverManagementAppService",
            "IDriverTaskAppService",
            "IInspectionAppService",
            "ILaundryCatalogAppService",
            "ILaundryHostAppService",
            "ILaundryOrderAppService",
            "ILaundryProfileAppService",
            "ILaundryStaffAppService",
            "ILaundryTimeSlotAppService"
        };

        appServiceInterfaces.Select(i => i.Name).OrderBy(n => n).ToArray().ShouldBe(expectedInterfaceNames);
        appServiceInterfaces.Count.ShouldBe(15);

        // Explicitly assert that forbidden/non-canonical interfaces do not exist
        contractsAssembly.GetType("laundry_SaaS.Orders.IOrderAdjustmentAppService").ShouldBeNull();
        contractsAssembly.GetType("laundry_SaaS.Notifications.ILaundryNotificationAppService").ShouldBeNull();
    }

    [Fact]
    public void Complaint_Attachment_Contracts_Must_Exist_With_Correct_Metadata()
    {
        var inputType = typeof(AddComplaintAttachmentInput);
        inputType.ShouldNotBeNull();
        var inputProps = inputType.GetProperties().Select(p => p.Name).ToList();
        inputProps.ShouldContain("BlobName");
        inputProps.ShouldContain("FileName");
        inputProps.ShouldContain("ContentType");

        var dtoType = typeof(ComplaintAttachmentDto);
        dtoType.ShouldNotBeNull();
        var dtoProps = dtoType.GetProperties().Select(p => p.Name).ToList();
        dtoProps.ShouldContain("Id");
        dtoProps.ShouldContain("ComplaintId");
        dtoProps.ShouldContain("BlobName");
        dtoProps.ShouldContain("FileName");
        dtoProps.ShouldContain("ContentType");
        dtoProps.ShouldContain("CreationTime");

        // Check IComplaintAppService method
        var method = typeof(IComplaintAppService).GetMethod("AddAttachmentAsync");
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(System.Threading.Tasks.Task<ComplaintAttachmentDto>));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(Guid));
        parameters[1].ParameterType.ShouldBe(typeof(AddComplaintAttachmentInput));

        // Check ComplaintDetailDto Attachments
        var detailProps = typeof(ComplaintDetailDto).GetProperties().Select(p => p.Name).ToList();
        detailProps.ShouldContain("Attachments");
    }

    [Fact]
    public void Bag_AppService_Must_Return_BagListDto_And_No_Phantom_BagDto()
    {
        var contractsAssembly = typeof(IBagAppService).Assembly;
        contractsAssembly.GetType("laundry_SaaS.Bags.BagDto").ShouldBeNull("BagDto does not exist; BagListDto is the canonical DTO.");

        var assignMethod = typeof(IBagAppService).GetMethod("AssignBagToOrderAsync");
        assignMethod.ShouldNotBeNull();
        assignMethod.ReturnType.ShouldBe(typeof(System.Threading.Tasks.Task<BagListDto>));
        var parameters = assignMethod.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void Deterministic_Dto_Inventory_And_Classification_Audit()
    {
        var contractsAssembly = typeof(ILaundryHostAppService).Assembly;
        var excludedTypeNames = new HashSet<string>
        {
            "laundry_SaaSApplicationContractsModule",
            "laundry_SaaSPermissions",
            "laundry_SaaSPermissionDefinitionProvider"
        };

        var allPublicClasses = contractsAssembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !excludedTypeNames.Contains(t.Name))
            .OrderBy(t => t.Name)
            .ToList();

        var inputs = allPublicClasses.Where(t => t.Name.EndsWith("Input")).ToList();
        var listsAndLookups = allPublicClasses.Where(t => !t.Name.EndsWith("Input") && (t.Name.EndsWith("ListDto") || t.Name.EndsWith("LookupDto"))).ToList();
        var outputsAndDetails = allPublicClasses.Where(t => !inputs.Contains(t) && !listsAndLookups.Contains(t)).ToList();

        // Deterministic classification check
        (inputs.Count + listsAndLookups.Count + outputsAndDetails.Count).ShouldBe(allPublicClasses.Count);

        inputs.Count.ShouldBe(40);
        listsAndLookups.Count.ShouldBe(12);
        outputsAndDetails.Count.ShouldBe(35);
        allPublicClasses.Count.ShouldBe(87);

        // PickupScheduleDto must be in outputsAndDetails
        outputsAndDetails.Select(t => t.Name).ShouldContain("PickupScheduleDto");

        // Complaint attachment DTOs
        inputs.Select(t => t.Name).ShouldContain("AddComplaintAttachmentInput");
        outputsAndDetails.Select(t => t.Name).ShouldContain("ComplaintAttachmentDto");

        // Ensure no AdjustmentItemDiffInput
        allPublicClasses.Select(t => t.Name).ShouldNotContain("AdjustmentItemDiffInput");

        // Write exact dump to file for verification
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== EXACT DTO INVENTORY (TOTAL: {allPublicClasses.Count}) ===");
        sb.AppendLine($"\n--- INPUT / COMMAND ({inputs.Count}) ---");
        for (int i = 0; i < inputs.Count; i++)
        {
            sb.AppendLine($"{i + 1:D2}. {inputs[i].Name}");
        }

        sb.AppendLine($"\n--- LIST / LOOKUP ({listsAndLookups.Count}) ---");
        for (int i = 0; i < listsAndLookups.Count; i++)
        {
            sb.AppendLine($"{i + 1:D2}. {listsAndLookups[i].Name}");
        }

        sb.AppendLine($"\n--- OUTPUT / DETAIL ({outputsAndDetails.Count}) ---");
        for (int i = 0; i < outputsAndDetails.Count; i++)
        {
            sb.AppendLine($"{i + 1:D2}. {outputsAndDetails[i].Name}");
        }

        System.IO.File.WriteAllText(@"C:\Users\MT\.gemini\antigravity-ide\brain\2dea50ac-4888-46aa-aae7-7dfa9631b91b\scratch\dto_inventory_dump.txt", sb.ToString());
    }

    [Fact]
    public void Verify_Suspicious_And_Orphan_Dtos()
    {
        var contractsAssembly = typeof(ILaundryHostAppService).Assembly;

        // Verify the 6 suspicious names:
        // We expect the explicit Domain-scoped names:
        contractsAssembly.GetType("laundry_SaaS.Customers.CreateCustomerAddressInput").ShouldNotBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.CreateLaundryItemTypeInput").ShouldNotBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.CreateLaundryServiceInput").ShouldNotBeNull();
        contractsAssembly.GetType("laundry_SaaS.Customers.UpdateCustomerAddressInput").ShouldNotBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.UpdateLaundryItemTypeInput").ShouldNotBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.UpdateLaundryServiceInput").ShouldNotBeNull();

        // And verify the generic short names do NOT exist:
        contractsAssembly.GetType("laundry_SaaS.Customers.CreateAddressInput").ShouldBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.CreateItemTypeInput").ShouldBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.CreateServiceInput").ShouldBeNull();
        contractsAssembly.GetType("laundry_SaaS.Customers.UpdateAddressInput").ShouldBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.UpdateItemTypeInput").ShouldBeNull();
        contractsAssembly.GetType("laundry_SaaS.Catalog.UpdateServiceInput").ShouldBeNull();

        // Now Audit Every Single DTO for References:
        var excludedTypeNames = new HashSet<string>
        {
            "laundry_SaaSApplicationContractsModule",
            "laundry_SaaSPermissions",
            "laundry_SaaSPermissionDefinitionProvider"
        };

        var allPublicClasses = contractsAssembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !excludedTypeNames.Contains(t.Name))
            .ToList();

        var referencedTypes = new HashSet<Type>();

        var appServiceInterfaces = contractsAssembly.GetExportedTypes()
            .Where(t => t.IsInterface && typeof(IApplicationService).IsAssignableFrom(t))
            .ToList();

        foreach (var iface in appServiceInterfaces)
        {
            foreach (var method in iface.GetMethods())
            {
                CollectReferencedTypes(method.ReturnType, referencedTypes);
                foreach (var param in method.GetParameters())
                {
                    CollectReferencedTypes(param.ParameterType, referencedTypes);
                }
            }
        }

        // Also collect recursively from properties of referenced DTOs
        bool addedNew;
        do
        {
            addedNew = false;
            foreach (var type in referencedTypes.ToList())
            {
                foreach (var prop in type.GetProperties())
                {
                    addedNew |= CollectReferencedTypes(prop.PropertyType, referencedTypes);
                }
            }
        } while (addedNew);

        var unreferencedDtos = allPublicClasses.Where(c => !referencedTypes.Contains(c)).ToList();

        var reportSb = new System.Text.StringBuilder();
        reportSb.AppendLine($"Total DTOs: {allPublicClasses.Count}");
        reportSb.AppendLine($"Referenced DTOs: {referencedTypes.Intersect(allPublicClasses).Count()}");
        reportSb.AppendLine($"Unreferenced (Orphan) DTOs: {unreferencedDtos.Count}");
        foreach (var orphan in unreferencedDtos)
        {
            reportSb.AppendLine($"  - {orphan.FullName}");
        }

        System.IO.File.WriteAllText(@"C:\Users\MT\.gemini\antigravity-ide\brain\2dea50ac-4888-46aa-aae7-7dfa9631b91b\scratch\dto_orphan_report.txt", reportSb.ToString());

        unreferencedDtos.ShouldBeEmpty($"Found orphan DTOs: {string.Join(", ", unreferencedDtos.Select(d => d.Name))}");
    }

    private static bool CollectReferencedTypes(Type type, HashSet<Type> referenced)
    {
        if (type == typeof(void) || type == typeof(System.Threading.Tasks.Task)) return false;

        bool added = false;
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                added |= CollectReferencedTypes(arg, referenced);
            }
            return added;
        }

        if (type.Assembly == typeof(ILaundryHostAppService).Assembly && type.IsClass && !type.IsAbstract)
        {
            return referenced.Add(type);
        }

        return false;
    }

    [Fact]
    public void Automated_Contract_Reference_Audit_Must_Have_Zero_Phantom_Types()
    {
        var contractsAssembly = typeof(ILaundryHostAppService).Assembly;
        var domainSharedAssembly = typeof(laundry_SaaS.Orders.OrderStatus).Assembly;

        var appServiceInterfaces = contractsAssembly.GetExportedTypes()
            .Where(t => t.IsInterface && typeof(IApplicationService).IsAssignableFrom(t))
            .ToList();

        foreach (var iface in appServiceInterfaces)
        {
            foreach (var method in iface.GetMethods())
            {
                // Verify return type
                VerifyTypeReference(method.ReturnType, contractsAssembly, domainSharedAssembly, $"{iface.Name}.{method.Name} return type");

                // Verify parameters
                foreach (var param in method.GetParameters())
                {
                    VerifyTypeReference(param.ParameterType, contractsAssembly, domainSharedAssembly, $"{iface.Name}.{method.Name}({param.Name})");
                }
            }
        }
    }

    private static void VerifyTypeReference(Type type, Assembly contractsAssembly, Assembly domainSharedAssembly, string context)
    {
        if (type == typeof(void)) return;

        // Unwrap Task<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>))
        {
            VerifyTypeReference(type.GetGenericArguments()[0], contractsAssembly, domainSharedAssembly, context);
            return;
        }

        if (type == typeof(System.Threading.Tasks.Task)) return;

        // Unwrap List<T>, IEnumerable<T>, PagedResultDto<T>
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                VerifyTypeReference(arg, contractsAssembly, domainSharedAssembly, context);
            }
            return;
        }

        // Check if primitive, basic .NET type, or ABP standard DTO
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(Guid) || type == typeof(TimeSpan) || type.IsEnum || type.Namespace?.StartsWith("System") == true ||
            type.Namespace?.StartsWith("Volo.Abp") == true)
        {
            return;
        }

        // Must belong to Application.Contracts or Domain.Shared assembly
        (type.Assembly == contractsAssembly || type.Assembly == domainSharedAssembly)
            .ShouldBeTrue($"Type {type.FullName} used in {context} must belong to either Application.Contracts or Domain.Shared");
    }
}

