use Std.Ops.Drop;
struct Foo['a] {
    
    kk: &'a mut i32
}


impl['a] Drop for Foo['a]{
    fn Drop(self: &uniq Self){
        *self.kk = *self.kk + 1;
        }
    }
fn main() -> i32 {
    let a = 5;
    let dd = make Foo { kk: &mut a };
    return a;
}