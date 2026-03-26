struct Foo {
    val: i32
}
fn main() -> i32 {
    let f = Foo { val = 99 };
    let r = &f;
    r.val
}
