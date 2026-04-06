trait Copy {}
impl Copy for i32 {}
impl Copy for bool {}
impl Copy for u8 {}
impl Copy for usize {}
impl[T:! type] Copy for &T {}
impl[T:! type] Copy for &const T {}
impl[T:! type] Copy for &mut T {}

impl[T:! type] Copy for *shared T {}
impl[T:! type] Copy for *const T {}
impl[T:! type] Copy for *mut T {}
impl[T:! type] Copy for *uniq T {}

trait Drop {
    fn Drop(self: &uniq Self);
}

trait MutReassign {}
impl MutReassign for i32 {}
impl MutReassign for bool {}
impl MutReassign for u8 {}
impl MutReassign for usize {}
impl[T:! type] MutReassign for &T {}
impl[T:! type] MutReassign for &const T {}
impl[T:! type] MutReassign for &mut T {}
impl[T:! type] MutReassign for &uniq T {}
impl[T:! type] MutReassign for *shared T {}
impl[T:! type] MutReassign for *const T {}
impl[T:! type] MutReassign for *mut T {}
impl[T:! type] MutReassign for *uniq T {}
