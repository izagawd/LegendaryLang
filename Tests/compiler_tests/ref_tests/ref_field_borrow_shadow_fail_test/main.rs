struct Foo {
    val: i32
}
impl Copy for Foo {}
fn main() -> i32 {
    let f = Foo { val = 5 };
    let r = &f.val;
    let f = Foo { val = 10 };
    *r
}
