struct Foo {
    val: i32
}
fn peek(r: &Foo) -> i32 {
    r.val
}
fn main() -> i32 {
    let f = Foo { val = 7 };
    peek(&f)
}
