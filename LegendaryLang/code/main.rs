use Std.Ops.Drop;
struct Foo['a] {
    
    kk: &'a mut i32
}

impl['a] Foo['a]{
    fn make_another_foo(self: &Self) -> Foo['a]{
            make Foo{
                kk: self.kk
                }
        }
    }
impl['a] Drop for Foo['a]{
    fn Drop(self: &uniq Self){
        *self.kk = *self.kk + 1;
        }
    }
fn main() -> i32 {
    let a = 5;
    let made = make Foo { kk: &mut a }.make_another_foo().make_another_foo();
   a
}