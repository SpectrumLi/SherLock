from typing import List
import re

class APISpecification():
    read_apis = []
    write_apis = []

    @classmethod
    def Initialize(cls):
        cls.read_apis.append('System\.Collections\.Generic\..?Dictionary.*::get_Item')
        cls.read_apis.append('System\.Collections\.Generic\..?Dictionary.*::get.*')
        cls.read_apis.append('System\.Collections\.Generic\..?Dictionary.*::ContainsKey')
        cls.read_apis.append('System\.Collections\.Generic\..?Dictionary.*::ContainsValue')
        cls.read_apis.append('System\.Collections\.Generic\..?Dictionary.*::TryGetValue')
        cls.write_apis.append('System\.Collections\.Generic\..?Dictionary.*::Add')
        cls.write_apis.append('System\.Collections\.Generic\..?Dictionary.*::Remove')
        cls.write_apis.append('System\.Collections\.Generic\..?Dictionary.*::Clear')
        cls.write_apis.append('System\.Collections\.Generic\..?Dictionary.*::set_Item')
        cls.write_apis.append('System\.Collections\.Generic\..?Dictionary.*::set.*')

        #cls.read_apis.append('System\.Collections\.Generic\.IDictionary.*::get_Item')
        #cls.read_apis.append('System\.Collections\.Generic\.IDictionary.*::ContainsKey')
        #cls.read_apis.append('System\.Collections\.Generic\.IDictionary.*::TryGetValue')
        #cls.write_apis.append('System\.Collections\.Generic\.IDictionary.*::Add')
        #cls.write_apis.append('System\.Collections\.Generic\.IDictionary.*::Remove')
        #cls.write_apis.append('System\.Collections\.Generic\.IDictionary.*::Clear')
        #cls.write_apis.append('System\.Collections\.Generic\.IDictionary.*::set_Item')

        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::BinarySearch.*?')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::Contains')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::ConvertAll')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::CopyTo')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::Exists')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::Find.*')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::GetRange')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::IndexOf')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::get_Item')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::MemberwiseClone')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::Take')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::Add.*?')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::Clear')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::Removei.*?')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::Reverse')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::Sort')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::ToArray')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::Insert')
        cls.write_apis.append('System\.Collections\.Generic\..*?List.*::set_Item')

        #cls.read_apis.append('System\.Collections\.Generic\.HashSet.*::Contains')
        #cls.read_apis.append('System\.Collections\.Generic\.HashSet.*::CopyTo')
        #cls.read_apis.append('System\.Collections\.Generic\.HashSet.*::Is.*Of')
        #cls.read_apis.append('System\.Collections\.Generic\.HashSet.*::MemberwiseClone')
        #cls.read_apis.append('System\.Collections\.Generic\.HashSet.*::Overlaps')
        #cls.write_apis.append('System\.Collections\.Generic\.HashSet.*::Add')
        #cls.write_apis.append('System\.Collections\.Generic\.HashSet.*::Remove')
        #cls.write_apis.append('System\.Collections\.Generic\.HashSet.*::.*With')
        #cls.write_apis.append('System\.Collections\.Generic\.HashSet.*::Clear')

        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::BinarySearch.*?')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::Contains')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::ConvertAll')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::CopyTo')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::Exists')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::Find.*')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::GetRange')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::IndexOf')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::get_Item')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::MemberwiseClone')
        #cls.read_apis.append('System\.Collections\.Generic\.IList.*::Take')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::Add')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::Clear')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::Remove')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::Reverse')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::Sort')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::ToArray')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::Insert')
        #cls.write_apis.append('System\.Collections\.Generic\.IList.*::set_Item')

        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::Clone')
        #cls.read_apis.append('System\.Collections\.Generic\.SortedList.*::Contains')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::GetByIndex')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::GetKey')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::GetValueList')
        cls.read_apis.append('System\.Collections\.Generic\..*?List.*::IndexOfValue')
        #cls.read_apis.append('System\.Collections\.Generic\.SortedList.*::get_Item')
        #cls.read_apis.append('System\.Collections\.Generic\.SortedList.*::MemberwiseClone')
        #cls.read_apis.append('System\.Collections\.Generic\.SortedList.*::Take')
        #cls.write_apis.append('System\.Collections\.Generic\.SortedList.*::Add')
        #cls.write_apis.append('System\.Collections\.Generic\.SortedList.*::Clear')
        #cls.write_apis.append('System\.Collections\.Generic\.SortedList.*::Remove')
        #cls.write_apis.append('System\.Collections\.Generic\.SortedList.*::set_Item')

        #cls.read_apis.append('System\.Collections\.Hashtable\.::Item.get.*')
        #cls.read_apis.append('System\.Collections\.Hashtable\.::Contains.*')
        #cls.read_apis.append('System\.Collections\.Hashtable\.::Clone')
        #cls.write_apis.append('System\.Collections\.Hashtable\.::Add')
        #cls.write_apis.append('System\.Collections\.Hashtable\.::Clear')
        #cls.write_apis.append('System\.Collections\.Hashtable\.::Remove')
        #cls.write_apis.append('System\.Collections\.Hashtable\.::set_Item')

        #cls.read_apis.append('System\.Collections\.ArrayList\.::BinarySearch')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::Clone')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::Contains')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::CopyTo')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::get_Item')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::.*IndexOf')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::GetRange')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::MemberWiseClone')
        #cls.read_apis.append('System\.Collections\.ArrayList\.::get_Count')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::Add')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::AddRange')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::Clear')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::Insert.*')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::Remove.*')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::Sort')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::Reverse')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::SetRange')
        #cls.write_apis.append('System\.Collections\.ArrayList\.::set_Item')

        #cls.read_apis.append('System\.Collections\.Generic\.LinkedList.*::Contains')
        #cls.read_apis.append('System\.Collections\.Generic\.LinkedList.*::Find.*')
        cls.read_apis.append('System\.Collections\.Generic\.LinkedList.*::get_Count')
        cls.read_apis.append('System\.Collections\.Generic\.LinkedList.*::get_First')
        cls.read_apis.append('System\.Collections\.Generic\.LinkedList.*::get_Last')
        #cls.write_apis.append('System\.Collections\.Generic\.LinkedList.*::Add.*')
        #cls.write_apis.append('System\.Collections\.Generic\.LinkedList.*::Clear')
        #cls.write_apis.append('System\.Collections\.Generic\.LinkedList.*::CopyTo')
        #cls.write_apis.append('System\.Collections\.Generic\.LinkedList.*::Remove.*')

        #cls.read_apis.append('')
        #cls.write_apis.append('')

    @classmethod
    def Wild_Match(cls, s: str, l :List[str]):
        for api in l:
            if re.search(api, s, re.I):
                #print(s, "match to", api)
                return True
        return False

    @classmethod
    def Is_Read_API(cls, s :str):
        return cls.Wild_Match(s, cls.read_apis)

    @classmethod
    def Is_Write_API(cls, s :str):
        return cls.Wild_Match(s, cls.write_apis)
