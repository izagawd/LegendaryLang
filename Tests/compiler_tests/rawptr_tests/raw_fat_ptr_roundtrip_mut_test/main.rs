use Std.Deref.Deref;
use Std.Ptr.GetMetadata;
use Std.Ptr.AddrEq;

fn main() -> i32 {
    let s = "world!";
    let r: &str = s.deref();
    let rp: *shared str = &raw *r;
    let back: &str = &*rp;
    let meta_before: usize = GetMetadata(rp);
    let meta_after: usize = GetMetadata(&raw *back);
    if meta_before == meta_after {
        if AddrEq(rp, &raw *back) {
            1
        } else {
            0
        }
    } else {
        0
    }
}
