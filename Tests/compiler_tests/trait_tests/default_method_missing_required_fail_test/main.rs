trait Ops {
    fn required(x: i32) -> i32;
    fn optional(x: i32) -> i32 {
        x + 1
    }
}

struct Bar {}
impl Copy for Bar {}
impl Ops for Bar {}

fn main() -> i32 { 0 }
