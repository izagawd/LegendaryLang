// f.transform().result() — transform(self:Self) consumes non-Copy Foo, returns Result.
// result(&Self) reads from Result. f is moved.
struct Foo { val: i32 }
struct Result { out: i32 }
impl Foo { fn transform(self: Self) -> Result { make Result { out: self.val } } }
impl Result { fn result(self: &Self) -> i32 { self.out } }
fn main() -> i32 {
    let f = make Foo { val: 42 };
    f.transform().result()
}
