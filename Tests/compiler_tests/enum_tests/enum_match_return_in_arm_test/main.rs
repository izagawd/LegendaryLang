enum Foo {
    A,
    B,
    C(i32)
}
fn main() -> i32 {
    let x = Foo.C(42);
    match x {
        Foo.A => 0,
        Foo.B => {
            4
        },
        Foo.C(val) => {
            return 44;
        }
    };
    4
}
