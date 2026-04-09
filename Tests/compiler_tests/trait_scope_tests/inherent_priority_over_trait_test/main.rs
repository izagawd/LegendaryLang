trait Getter {
    fn get(self: &Self) -> i32;
}
struct Foo { val: i32 }
impl Foo {
    fn get(self: &Self) -> i32 { 100 }
}
impl Getter for Foo {
    fn get(self: &Self) -> i32 { 200 }
}
fn main() -> i32 {
    let f = make Foo { val: 0 };
    f.get()
}
