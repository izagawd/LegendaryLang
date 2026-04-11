struct Foo { kk: i32 }
struct Holder['a] { r: &'a Foo }
fn TakeOwnership[T:! Sized](input: T) {}

fn good() -> Foo {
    let a = make Foo { kk: 5 };
    let dd = make Holder { r: &a };
    TakeOwnership(dd);
    return a;
}

fn main() -> i32 { good().kk }
