trait Ops {
    fn required(x: i32) -> i32;
    fn optional(x: i32) -> i32 {
        x + 1
    }
}

struct Bar {}
impl Copy for Bar {}
impl Ops for Bar {
    fn required(x: i32) -> i32 {
        x * 2
    }
}

fn main() -> i32 {
    let r = (Bar as Ops).required(10);
    let o = (Bar as Ops).optional(10);
    r + o
}
