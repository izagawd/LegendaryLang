struct Foo { val: i32 }
struct Holder['a] { r: &'a Foo }
fn TakeOwnership[T:! type](input: T) {}
fn main() -> i32 {
    let a = make Foo { val: 5 };
    let b = make Foo { val: 10 };
    let h = make Holder { r: &a };
    h = make Holder { r: &b };
    TakeOwnership(a);
    h.r.val
}
