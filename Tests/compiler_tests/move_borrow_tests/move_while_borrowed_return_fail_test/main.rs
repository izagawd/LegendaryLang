struct Foo { val: i32 }
struct Holder['a] { r: &'a Foo }
fn bad() -> Foo {
    let a = make Foo { val: 5 };
    let h = make Holder { r: &a };
    a
}
fn main() -> i32 { bad().val }
