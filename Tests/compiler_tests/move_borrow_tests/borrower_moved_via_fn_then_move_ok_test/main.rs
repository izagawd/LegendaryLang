struct Foo { kk: i32 }
struct Holder['a] { r: &'a Foo }
fn TakeOwnership[T:! type](input: T) {}

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    let h = make Holder { r: &a };
    TakeOwnership(h);
    TakeOwnership(a);
    5
}
