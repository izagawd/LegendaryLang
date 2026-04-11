use Std.Deref.Deref;
use Std.Ptr.AddrEq;
use Std.Ptr.GetMetadata;

fn main() -> i32 {
    let s = "hello";
    let r: &str = s.deref();
    let rp: *shared str = &raw *r;
    let meta: usize = GetMetadata(rp);
    if meta == 5 {
        if AddrEq(rp, &raw *r) {
            1
        } else {
            0
        }
    } else {
        0
    }
}
