struct Foo { kk: i32 }
struct Holder['a] { r: &'a Foo }
fn TakeOwnership[T:! Sized](input: T) {}

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    TakeOwnership(make Holder { r: &a });
    TakeOwnership(a);
    5
}
