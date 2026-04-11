use Std.Deref.Deref;
use Std.Ptr.GetMetadata;

fn main() -> i32 {
    let s = "hello";
    let r: &str = s.deref();
    let rp: *shared str = &raw *r;
    let back: &str = &*rp;
    let meta: usize = GetMetadata(&raw *back);
    if meta == 5 {
        1
    } else {
        0
    }
}
