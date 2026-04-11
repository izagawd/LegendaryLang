struct Foo { val: i32 }
fn TakeOwnership[T:! Sized](input: T) {}
fn main() -> i32 {
    let a = make Foo { val: 5 };
    let r: &Foo = &a;
    let v = r.val;
    TakeOwnership(a);
    v
}
