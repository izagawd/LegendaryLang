use Std.Mem.SizeOf;

fn main() -> i32 {
    let fat: usize = SizeOf(&const str);
    let thin: usize = SizeOf(&const ());
    if fat == thin * 2 {
        1
    } else {
        0
    }
}
