struct Foo { val: i32 }
struct Bar { val: i32 }
impl Copy for Foo {}
impl Copy for Bar {}
fn take_foo(f: Foo) -> i32 { f.val }
fn main() -> i32 {
    let b = make Bar { val: 42 };
    take_foo(b)
}
