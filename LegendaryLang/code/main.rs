
use code::dark::next;

struct Bro{}

fn main() -> i32{
    fn return_int() -> i32{
        return 5;
     }
    return return_int();
}

