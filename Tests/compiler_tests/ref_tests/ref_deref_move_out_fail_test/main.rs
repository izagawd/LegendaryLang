struct Foo {
    val: i32
}
fn main() -> i32 {
    let f = Foo { val = 5 };
    let r = &f;
    let bruh = *r;
    5
}
