struct Foo { kk: i32 }
struct Inner['a] { r: &'a Foo }
struct Outer['a] { inner: Inner['a] }
fn TakeOwnership[T:! type](input: T) {}

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    let o = make Outer { inner: make Inner { r: &a } };
    TakeOwnership(a);
    5
}
